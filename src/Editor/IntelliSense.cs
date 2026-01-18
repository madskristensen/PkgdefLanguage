using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Win32;

namespace PkgdefLanguage
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    public class IntelliSense : IAsyncCompletionSourceProvider
    {
        public IAsyncCompletionSource GetOrCreate(ITextView textView) =>
            textView.Properties.GetOrCreateSingletonProperty(() => new AsyncCompletionSource());
    }

    public class AsyncCompletionSource : IAsyncCompletionSource
    {
        private static readonly ImageElement _referenceIcon = new(KnownMonikers.LocalVariable.ToImageId(), "Variable");
        private static readonly ImageElement _propertyIcon = new(KnownMonikers.Property.ToImageId(), "Property");

        // Cache completion items for variables since they don't change
        private static ImmutableArray<CompletionItem> _cachedVariableCompletions;
        private static readonly object _cacheLock = new object();

        public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken cancellationToken)
        {
            Document document = session.TextView.TextBuffer.GetDocument();
            ParseItem item = document.FindItemFromPosition(triggerLocation.Position);
            IEnumerable<CompletionItem> items = null;

            if (item?.Type == ItemType.Reference)
            {
                items = GetReferenceCompletion();
            }
            else if (item?.Type == ItemType.RegistryKey)
            {
                items = GetRegistryKeyCompletion(item, triggerLocation);
            }
            else if (item?.Type == ItemType.String || item?.Type == ItemType.Literal || IsPropertyNamePosition(document, triggerLocation))
            {
                items = GetRegistryValueCompletion(document, triggerLocation);
            }

            return Task.FromResult(items == null ? null : new CompletionContext(items.ToImmutableArray()));
        }

        private IEnumerable<CompletionItem> GetRegistryKeyCompletion(ParseItem item, SnapshotPoint triggerLocation)
        {
            ITextSnapshotLine line = triggerLocation.GetContainingLine();
            var column = triggerLocation.Position - line.Start - 1;
            var previousKey = item.Text.LastIndexOf('\\', column);

            if (previousKey > -1)
            {
                IEnumerable<string> prevKeys = item.Text.Substring(0, previousKey).Split('\\').Skip(1);
                RegistryKey root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration);
                RegistryKey parent = root;
                var keysToDispose = new List<RegistryKey>();

                try
                {
                    foreach (var subKey in prevKeys)
                    {
                        var nextKey = parent.OpenSubKey(subKey);
                        if (nextKey == null)
                        {
                            return null; // Path doesn't exist
                        }
                        
                        if (parent != root) // Don't dispose the root key
                        {
                            keysToDispose.Add(parent);
                        }
                        parent = nextKey;
                    }

                    return parent?.GetSubKeyNames()?.Select(s => new CompletionItem(s, this, _referenceIcon));
                }
                finally
                {
                    // Properly dispose all opened registry keys
                    foreach (var keyToDispose in keysToDispose)
                    {
                        keyToDispose?.Dispose();
                    }
                    
                                if (parent != root)
                                {
                                    parent?.Dispose();
                                }
                            }
                        }

                            return null;
                        }

                        private bool IsPropertyNamePosition(Document document, SnapshotPoint triggerLocation)
                        {
                            ITextSnapshotLine line = triggerLocation.GetContainingLine();
                            string lineText = line.GetText();
                            int positionInLine = triggerLocation.Position - line.Start.Position;

                            // Check if we're before the = sign (property name side, not value side)
                            int equalsIndex = lineText.IndexOf('=');
                            if (equalsIndex > -1 && positionInLine >= equalsIndex)
                            {
                                return false; // We're on the right side of =, so no completion
                            }

                            string trimmedLineText = lineText.TrimStart();

                            // Allow completion if:
                            // 1. Line is blank/whitespace (starting a new property)
                            // 2. Line starts with @ or " (typing a property name)
                            // 3. Line doesn't start with [ (not a registry key) or ; (not a comment) or # (not a preprocessor)
                            if (trimmedLineText.Length > 0)
                            {
                                if (trimmedLineText.StartsWith("[") || 
                                    trimmedLineText.StartsWith(";") || 
                                    trimmedLineText.StartsWith("//") ||
                                    trimmedLineText.StartsWith("#"))
                                {
                                    return false;
                                }
                            }

                            // Make sure we have a registry key above us
                            Entry currentEntry = FindCurrentEntry(document, triggerLocation);
                            return currentEntry != null;
                        }

                        private Entry FindCurrentEntry(Document document, SnapshotPoint triggerLocation)
                        {
                            // Find the most recent entry (registry key section) before this position
                            return document.Items
                                .OfType<Entry>()
                                .Where(e => e.RegistryKey.Span.Start < triggerLocation.Position)
                                .OrderByDescending(e => e.RegistryKey.Span.Start)
                                .FirstOrDefault();
                        }

                        private IEnumerable<CompletionItem> GetRegistryValueCompletion(Document document, SnapshotPoint triggerLocation)
                        {
                            Entry currentEntry = FindCurrentEntry(document, triggerLocation);
                            if (currentEntry?.RegistryKey == null)
                            {
                                return null;
                            }

                            // Extract the registry key path from the current entry
                            string keyPath = ExtractRegistryKeyPath(currentEntry.RegistryKey.Text);
                            if (string.IsNullOrEmpty(keyPath))
                            {
                                return null;
                            }

                            // Get the parent key path (everything except the last segment)
                            int lastBackslash = keyPath.LastIndexOf('\\');
                            if (lastBackslash == -1)
                            {
                                return null; // No parent key
                            }

                            string parentKeyPath = keyPath.Substring(0, lastBackslash);

                            // Collect all value names from sibling keys
                            var valueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            try
                            {
                                RegistryKey root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration);
                                RegistryKey parentKey = OpenRegistryKeyPath(root, parentKeyPath);

                                if (parentKey != null)
                                {
                                    try
                                    {
                                        // Get all subkeys (siblings)
                                        string[] subKeyNames = parentKey.GetSubKeyNames();
                                        if (subKeyNames != null)
                                        {
                                            foreach (string subKeyName in subKeyNames)
                                            {
                                                using (RegistryKey siblingKey = parentKey.OpenSubKey(subKeyName))
                                                {
                                                    if (siblingKey != null)
                                                    {
                                                        // Get all value names in this sibling key
                                                        string[] valueNameArray = siblingKey.GetValueNames();
                                                        if (valueNameArray != null)
                                                        {
                                                            foreach (string valueName in valueNameArray)
                                                            {
                                                                // Empty string means default value (@)
                                                                if (string.IsNullOrEmpty(valueName))
                                                                {
                                                                    valueNames.Add("@");
                                                                }
                                                                else
                                                                {
                                                                    valueNames.Add(valueName);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (parentKey != root)
                                        {
                                            parentKey.Dispose();
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // If we can't access the registry, just return null
                                return null;
                            }

                            // Filter out @ if there are already properties defined
                            // (@ can only be the first property after the registry key)
                            if (currentEntry.Properties.Count > 0)
                            {
                                valueNames.Remove("@");
                            }

                            // Return completion items
                            if (valueNames.Count > 0)
                            {
                                var completions = new List<CompletionItem>();

                                // Add @ first if it exists
                                if (valueNames.Contains("@"))
                                {
                                    completions.Add(new CompletionItem("@", this, _propertyIcon, ImmutableArray<CompletionFilter>.Empty, string.Empty, "@", "@", "@", ImmutableArray<ImageElement>.Empty));
                                }

                                // Add other properties alphabetically
                                foreach (var name in valueNames.Where(n => n != "@").OrderBy(n => n))
                                {
                                    // Display name without quotes, but insert text with quotes
                                    string displayText = name;
                                    string insertText = $"\"{name}\"";
                                    completions.Add(new CompletionItem(displayText, this, _propertyIcon, ImmutableArray<CompletionFilter>.Empty, string.Empty, insertText, displayText, displayText, ImmutableArray<ImageElement>.Empty));
                                }

                                return completions;
                            }

                            return null;
                        }

                        private string ExtractRegistryKeyPath(string registryKeyText)
                        {
                            // Remove [ and ] and trim
                            string path = registryKeyText.Trim().TrimStart('[').TrimEnd(']').Trim();

                            // Expand $RootKey$ or $rootkey$ (case-insensitive)
                            if (path.IndexOf("$rootkey$", StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                // Find and replace $RootKey$ or $rootkey$ case-insensitively
                                int index = path.IndexOf("$rootkey$", StringComparison.OrdinalIgnoreCase);
                                if (index >= 0)
                                {
                                    int length = "$rootkey$".Length;
                                    path = path.Remove(index, length).TrimStart('\\');
                                }
                            }

                            return path;
                        }

                        private RegistryKey OpenRegistryKeyPath(RegistryKey root, string path)
                        {
                            if (string.IsNullOrEmpty(path))
                            {
                                return root;
                            }

                            string[] segments = path.Split('\\');
                            RegistryKey current = root;

                            foreach (string segment in segments)
                            {
                                if (string.IsNullOrEmpty(segment))
                                {
                                    continue;
                                }

                                RegistryKey next = current.OpenSubKey(segment);
                                if (next == null)
                                {
                                    // Path doesn't exist
                                    if (current != root)
                                    {
                                        current.Dispose();
                                    }
                                    return null;
                                }

                                if (current != root)
                                {
                                    current.Dispose();
                                }
                                current = next;
                            }

                            return current;
                        }

                        private IEnumerable<CompletionItem> GetReferenceCompletion()
        {
            // Thread-safe lazy initialization of cached completion items
            if (_cachedVariableCompletions.IsDefault)
            {
                lock (_cacheLock)
                {
                    if (_cachedVariableCompletions.IsDefault)
                    {
                        var completions = new List<CompletionItem>();
                        foreach (var key in PredefinedVariables.Variables.Keys)
                        {
                            var completion = new CompletionItem(key, this, _referenceIcon, ImmutableArray<CompletionFilter>.Empty, "", $"${key}$", key, key, ImmutableArray<ImageElement>.Empty);
                            completion.Properties.AddProperty("description", PredefinedVariables.Variables[key]);
                            completions.Add(completion);
                        }
                        _cachedVariableCompletions = completions.ToImmutableArray();
                    }
                }
            }
            
            return _cachedVariableCompletions;
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            if (item.Properties.TryGetProperty("description", out string description))
            {
                return Task.FromResult<object>(description);
            }

            return Task.FromResult<object>(null);
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            if (trigger.Character == '\n' || trigger.Character == ']' || trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            Document document = triggerLocation.Snapshot.TextBuffer.GetDocument();
            ParseItem item = document?.FindItemFromPosition(triggerLocation.Position);

            if (item?.Type == ItemType.Reference)
            {
                var tokenSpan = new SnapshotSpan(triggerLocation.Snapshot, item);
                return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
            }
            else if (item?.Type == ItemType.RegistryKey && item.Text.IndexOf("$rootkey$", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var column = triggerLocation.Position - item.Span.Start;

                if (column < 1)
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                var start = item.Text.LastIndexOf('\\', column - 1) + 1;
                var end = item.Text.IndexOf('\\', column);
                var close = item.Text.IndexOf(']', column);
                var textEnd = item.Text.IndexOf(']', column);
                end = end >= start ? end : close;
                end = end >= start ? end : textEnd;
                end = end >= start ? end : item.Text.TrimEnd().Length;

                if (end >= start)
                {
                    if (close == -1 || column <= close)
                    {
                        var span = new SnapshotSpan(triggerLocation.Snapshot, item.Span.Start + start, end - start);
                        return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
                    }
                }
            }
            else if (item?.Type == ItemType.String || item?.Type == ItemType.Literal || IsPropertyNamePosition(document, triggerLocation))
            {
                // Only provide completion if we're at a property name position
                if (!IsPropertyNamePosition(document, triggerLocation))
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                // Handle property name completion
                ITextSnapshotLine line = triggerLocation.GetContainingLine();
                string lineText = line.GetText();
                int lineStartOffset = line.Start.Position;
                int positionInLine = triggerLocation.Position - lineStartOffset;

                // Check if we're before the = sign
                int equalsIndex = lineText.IndexOf('=');
                if (equalsIndex > -1 && positionInLine >= equalsIndex)
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }

                // Find the start of the property name (@ or opening quote)
                int start = lineText.IndexOf('@');
                if (start == -1)
                {
                    start = lineText.IndexOf('"');
                }

                if (start > -1 && positionInLine >= start)
                {
                    // Find the end (either the closing quote, = sign, or end of typed text)
                    int end = positionInLine;
                    if (start < lineText.Length && lineText[start] == '"')
                    {
                        // For quoted strings, look for closing quote
                        int closingQuote = lineText.IndexOf('"', start + 1);
                        if (closingQuote > start && closingQuote >= positionInLine)
                        {
                            end = closingQuote;
                        }
                    }
                    else
                    {
                        // For @, go until = or whitespace
                        int equalsSign = lineText.IndexOf('=', start);
                        if (equalsSign > start)
                        {
                            end = equalsSign;
                            // Trim whitespace before =
                            while (end > start && char.IsWhiteSpace(lineText[end - 1]))
                            {
                                end--;
                            }
                        }
                    }

                    if (end > start)
                    {
                        var span = new SnapshotSpan(triggerLocation.Snapshot, lineStartOffset + start, end - start);
                        return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
                    }
                }
                else
                {
                    // Blank line or no @ or " yet - use current position as span start
                    var span = new SnapshotSpan(triggerLocation.Snapshot, triggerLocation.Position, 0);
                    return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
                }
            }

            return CompletionStartData.DoesNotParticipateInCompletion;
        }
    }
}