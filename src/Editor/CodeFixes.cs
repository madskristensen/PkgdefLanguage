using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PkgdefLanguage
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName + " Suggested Actions")]
    internal class CodeFixProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new CodeFixSource(textView, textBuffer));
        }
    }

    internal class CodeFixSource : ISuggestedActionsSource
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;

        public CodeFixSource(ITextView textView, ITextBuffer textBuffer)
        {
                _textView = textView;
                _textBuffer = textBuffer;
            }

            public event EventHandler<EventArgs> SuggestedActionsChanged { add { } remove { } }

            public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                Document document = _textBuffer.GetDocument();
                if (document == null)
                {
                    return Task.FromResult(false);
                }

                // Check for refactoring actions on registry keys
                Entry entryAtPosition = FindEntryAtPosition(document, range.Start.Position);
                if (entryAtPosition != null)
                {
                    return Task.FromResult(true);
                }

                // Find all items with errors at this range (not just a position)
                var itemsWithErrors = FindAllItemsWithErrorsInRange(document, range.Start.Position, range.End.Position);

                if (!itemsWithErrors.Any())
                {
                    return Task.FromResult(false);
                }

                // Check if any of the errors have quick fixes available
                bool hasQuickFixes = itemsWithErrors.Any(item =>
                    item.Errors.Any(e =>
                        e.ErrorCode == "PL002" ||
                        e.ErrorCode == "PL003" ||
                        e.ErrorCode == "PL004" ||
                        e.ErrorCode == "PL005" ||
                        e.ErrorCode == "PL006" ||
                        e.ErrorCode == "PL007" ||
                        e.ErrorCode == "PL008"));

                return Task.FromResult(hasQuickFixes);
            }

            public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            {
                return GetSuggestedActions(range);
            }

            private IEnumerable<SuggestedActionSet> GetSuggestedActions(SnapshotSpan range)
            {
                Document document = _textBuffer.GetDocument();
                        if (document == null)
                        {
                            yield break;
                    }

                    var actions = new List<ISuggestedAction>();
                    var refactoringActions = new List<ISuggestedAction>();

                    // Check for refactoring actions on registry keys
                    Entry entryAtPosition = FindEntryAtPosition(document, range.Start.Position);
                    if (entryAtPosition != null)
                    {
                        // Add default value action (only if no @ property exists)
                        bool hasDefaultValue = entryAtPosition.Properties.Any(p => p.Name.Text.Trim() == "@");
                        if (!hasDefaultValue)
                        {
                            refactoringActions.Add(new AddDefaultValueAction(_textView, _textBuffer, entryAtPosition));
                        }

                        // Sort properties action (only if there are 2+ properties)
                        if (entryAtPosition.Properties.Count >= 2)
                        {
                            refactoringActions.Add(new SortPropertiesAction(_textView, _textBuffer, entryAtPosition));
                        }
                    }

                    // Find all items with errors in this range (not just at a position)
                    var itemsWithErrors = FindAllItemsWithErrorsInRange(document, range.Start.Position, range.End.Position);

                    // Collect quick fixes from all items with errors at this position
                    foreach (var itemWithError in itemsWithErrors)
                    {
                        foreach (Error error in itemWithError.Errors)
                        {
                            switch (error.ErrorCode)
                            {
                                case "PL002": // Unclosed registry key
                                    actions.Add(new AddClosingBracketAction(_textView, _textBuffer, itemWithError));
                                    break;

                                case "PL003": // Forward slash in registry path
                                    actions.Add(new ReplaceForwardSlashAction(_textView, _textBuffer, itemWithError));
                                    break;

                                case "PL004": // Quoted @ sign
                                    actions.Add(new RemoveQuotesFromAtSignAction(_textView, _textBuffer, itemWithError));
                                    break;

                                case "PL005": // Missing quotes on property name OR missing closing quote
                                    if (itemWithError.Type == ItemType.String)
                                    {
                                        // String missing closing quote
                                        actions.Add(new AddClosingQuoteAction(_textView, _textBuffer, itemWithError));
                                    }
                                    else
                                    {
                                        // Unquoted property name
                                        actions.Add(new SurroundWithQuotesAction(_textView, _textBuffer, itemWithError));
                                    }
                                    break;

                                case "PL006": // Unknown variable - suggest similar
                                    var suggestAction = new SuggestSimilarVariableAction(_textView, _textBuffer, itemWithError);
                                    if (suggestAction.HasSuggestion)
                                    {
                                        actions.Add(suggestAction);
                                    }
                                    break;

                                        case "PL007": // Variable missing closing $
                                                actions.Add(new AddClosingDollarSignAction(_textView, _textBuffer, itemWithError));
                                                break;

                                            case "PL008": // Duplicate registry key
                                                actions.Add(new ConsolidateDuplicateKeysAction(_textView, _textBuffer, itemWithError, document));
                                                break;
                                        }
                                    }
                                }

                    // Return quick fixes first (higher priority)
                    if (actions.Any())
                    {
                        yield return new SuggestedActionSet(null, actions, null, SuggestedActionSetPriority.Medium);
                    }

                    // Return refactoring actions (lower priority)
                    if (refactoringActions.Any())
                    {
                        yield return new SuggestedActionSet("Refactoring", refactoringActions, null, SuggestedActionSetPriority.Low);
                    }
                }

                private Entry FindEntryAtPosition(Document document, int position)
                {
                    foreach (var item in document.Items)
                    {
                        if (item is Entry entry && entry.Span.Contains(position))
                        {
                            return entry;
                        }
                    }
                    return null;
                }

        private List<ParseItem> FindAllItemsWithErrorsInRange(Document document, int rangeStart, int rangeEnd)
        {
            var itemsWithErrors = new List<ParseItem>();
            var addedSpans = new HashSet<(int Start, int Length)>(); // Track spans to avoid duplicates

            // Search through all items in the document
            foreach (var item in document.Items)
            {
                // If it's an Entry, prioritize checking children over the Entry itself
                if (item is Entry entry)
                {
                    // Check registry key - does the range overlap with this span?
                    if (entry.RegistryKey != null &&
                        entry.RegistryKey.Errors.Any() &&
                        RangeOverlapsSpan(rangeStart, rangeEnd, entry.RegistryKey.Span))
                    {
                        var spanKey = (entry.RegistryKey.Span.Start, entry.RegistryKey.Span.Length);
                        if (addedSpans.Add(spanKey))
                        {
                            itemsWithErrors.Add(entry.RegistryKey);
                        }
                    }

                    // Check references within registry key (e.g., $RootKey$ in [$RootKey\Path])
                    if (entry.RegistryKey != null && entry.RegistryKey.References.Any())
                    {
                        foreach (var reference in entry.RegistryKey.References)
                        {
                            if (reference.Errors.Any() && RangeOverlapsSpan(rangeStart, rangeEnd, reference.Span))
                            {
                                var spanKey = (reference.Span.Start, reference.Span.Length);
                                if (addedSpans.Add(spanKey))
                                {
                                    itemsWithErrors.Add(reference);
                                }
                            }
                        }
                    }

                    // Check all properties
                    foreach (var property in entry.Properties)
                    {
                        // Check property name
                        if (property.Name != null &&
                            property.Name.Errors.Any() &&
                            RangeOverlapsSpan(rangeStart, rangeEnd, property.Name.Span))
                        {
                            var spanKey = (property.Name.Span.Start, property.Name.Span.Length);
                            if (addedSpans.Add(spanKey))
                            {
                                itemsWithErrors.Add(property.Name);
                            }
                        }

                        // Check property value
                        if (property.Value != null &&
                            property.Value.Errors.Any() &&
                            RangeOverlapsSpan(rangeStart, rangeEnd, property.Value.Span))
                        {
                            var spanKey = (property.Value.Span.Start, property.Value.Span.Length);
                            if (addedSpans.Add(spanKey))
                            {
                                itemsWithErrors.Add(property.Value);
                            }
                        }

                        // Check references within property name
                        if (property.Name != null && property.Name.References.Any())
                        {
                            foreach (var reference in property.Name.References)
                            {
                                if (reference.Errors.Any() && RangeOverlapsSpan(rangeStart, rangeEnd, reference.Span))
                                {
                                    var spanKey = (reference.Span.Start, reference.Span.Length);
                                    if (addedSpans.Add(spanKey))
                                    {
                                        itemsWithErrors.Add(reference);
                                    }
                                }
                            }
                        }

                        // Check references within property value
                        if (property.Value != null && property.Value.References.Any())
                        {
                            foreach (var reference in property.Value.References)
                            {
                                if (reference.Errors.Any() && RangeOverlapsSpan(rangeStart, rangeEnd, reference.Span))
                                {
                                    var spanKey = (reference.Span.Start, reference.Span.Length);
                                    if (addedSpans.Add(spanKey))
                                    {
                                        itemsWithErrors.Add(reference);
                                    }
                                }
                            }
                        }
                    }
                }
                // For non-Entry items, check the item itself
                else if (item.Errors.Any() && RangeOverlapsSpan(rangeStart, rangeEnd, item.Span))
                {
                    var spanKey = (item.Span.Start, item.Span.Length);
                    if (addedSpans.Add(spanKey))
                    {
                        itemsWithErrors.Add(item);
                    }

                    // Check references on this item
                    if (item.References.Any())
                    {
                        foreach (var reference in item.References)
                        {
                            if (reference.Errors.Any() && RangeOverlapsSpan(rangeStart, rangeEnd, reference.Span))
                            {
                                var refSpanKey = (reference.Span.Start, reference.Span.Length);
                                if (addedSpans.Add(refSpanKey))
                                {
                                    itemsWithErrors.Add(reference);
                                }
                            }
                        }
                    }
                }
            }

            return itemsWithErrors;
        }

        // Check if a range overlaps with a span
        // Returns true if any part of the range intersects with the span
        private bool RangeOverlapsSpan(int rangeStart, int rangeEnd, Span span)
        {
            // Range overlaps if:
            // - Range starts within the span, OR
            // - Range ends within the span, OR
            // - Range completely contains the span, OR
            // - Range is immediately adjacent to span end (for cursor at end of line)
            return (rangeStart >= span.Start && rangeStart <= span.End + 1) ||
                   (rangeEnd >= span.Start && rangeEnd <= span.End + 1) ||
                   (rangeStart <= span.Start && rangeEnd >= span.End);
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    // Base class for all code fix actions
    internal abstract class CodeFixAction : ISuggestedAction
    {
        protected readonly ITextView _textView;
        protected readonly ITextBuffer _textBuffer;
        protected readonly ParseItem _item;

        protected CodeFixAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
        {
            _textView = textView;
            _textBuffer = textBuffer;
            _item = item;
        }

        public abstract string DisplayText { get; }

        public bool HasActionSets => false;
        public string IconAutomationText => null;
        public System.Windows.Input.ICommand IconCommand => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasPreview => false;

        public abstract void Invoke(CancellationToken cancellationToken);

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    // PL002: Add missing closing bracket
    internal class AddClosingBracketAction : CodeFixAction
    {
        public AddClosingBracketAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
            : base(textView, textBuffer, item)
        {
        }

        public override string DisplayText => "Add closing ]";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = _item.Text.TrimEnd();

            if (!text.EndsWith("]"))
            {
                var endPosition = _item.Span.Start + text.Length;
                _textBuffer.Insert(endPosition, "]");
            }
        }
    }

    // PL003: Replace forward slash with backslash
    internal class ReplaceForwardSlashAction : CodeFixAction
    {
        public ReplaceForwardSlashAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
            : base(textView, textBuffer, item)
        {
        }

        public override string DisplayText => "Replace / with \\";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var span = new Span(_item.Span.Start, _item.Span.Length);
            var newText = _item.Text.Replace('/', '\\');

            _textBuffer.Replace(span, newText);
        }
    }

    // PL004: Remove quotes from @ sign
    internal class RemoveQuotesFromAtSignAction : CodeFixAction
    {
        public RemoveQuotesFromAtSignAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
            : base(textView, textBuffer, item)
        {
        }

        public override string DisplayText => "Remove quotes from @";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var span = new Span(_item.Span.Start, _item.Span.Length);

            // Replace "@" with @
            _textBuffer.Replace(span, "@");
        }
    }

        // PL005: Surround with quotes
        internal class SurroundWithQuotesAction : CodeFixAction
        {
            public SurroundWithQuotesAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
                : base(textView, textBuffer, item)
            {
            }

            public override string DisplayText => "Surround with quotes";

            public override void Invoke(CancellationToken cancellationToken)
            {
                var span = new Span(_item.Span.Start, _item.Span.Length);
                var trimmedText = _item.Text.Trim();

                // Wrap the text in quotes
                var newText = _item.Text.Replace(trimmedText, $"\"{trimmedText}\"");
                _textBuffer.Replace(span, newText);
            }
        }

            // PL005: Add missing closing quote for strings
            internal class AddClosingQuoteAction : CodeFixAction
            {
                public AddClosingQuoteAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
                    : base(textView, textBuffer, item)
                {
                }

                public override string DisplayText => "Add closing \"";

                public override void Invoke(CancellationToken cancellationToken)
                {
                    var snapshot = _textBuffer.CurrentSnapshot;
                    var text = snapshot.GetText(_item.Span.Start, _item.Span.Length);

                    // Check if this is a property name (has = after it) or a property value
                    var equalsIndex = text.IndexOf('=');

                    int insertPosition;
                    if (equalsIndex > 0)
                    {
                        // Property name - add quote before the =
                        // Find the position before = (trim any whitespace before =)
                        var beforeEquals = text.Substring(0, equalsIndex).TrimEnd();
                        insertPosition = _item.Span.Start + beforeEquals.Length;
                    }
                    else
                    {
                        // Property value or other string - add at the end (before newlines)
                        var trimmedEnd = text.TrimEnd('\r', '\n', ' ', '\t');
                        insertPosition = _item.Span.Start + trimmedEnd.Length;
                    }

                                        _textBuffer.Insert(insertPosition, "\"");
                                    }
                                }

                        // PL006: Suggest similar variable name
                        internal class SuggestSimilarVariableAction : CodeFixAction
                        {
                            private readonly string _suggestion;

                            public SuggestSimilarVariableAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
                                : base(textView, textBuffer, item)
                            {
                                _suggestion = FindSimilarVariable(item.Text.Trim().Trim('$'));
                            }

                            public bool HasSuggestion => !string.IsNullOrEmpty(_suggestion);

                            public override string DisplayText => $"Change to ${_suggestion}$";

                            public override void Invoke(CancellationToken cancellationToken)
                            {
                                if (string.IsNullOrEmpty(_suggestion))
                                {
                                    return;
                                }

                                var span = new Span(_item.Span.Start, _item.Span.Length);
                                _textBuffer.Replace(span, $"${_suggestion}$");
                            }

                            private static string FindSimilarVariable(string input)
                            {
                                if (string.IsNullOrEmpty(input))
                                {
                                    return null;
                                }

                                string bestMatch = null;
                                int bestDistance = int.MaxValue;
                                var inputLower = input.ToLowerInvariant();

                                foreach (var variable in PredefinedVariables.Variables.Keys)
                                {
                                    var variableLower = variable.ToLowerInvariant();

                                    // Check for substring match first (e.g., "Root" matches "RootFolder")
                                    if (variableLower.Contains(inputLower) || inputLower.Contains(variableLower))
                                    {
                                        return variable;
                                    }

                                    // Calculate Levenshtein distance for fuzzy matching
                                    int distance = LevenshteinDistance(inputLower, variableLower);
                                    if (distance < bestDistance && distance <= Math.Max(input.Length, variable.Length) / 2)
                                    {
                                        bestDistance = distance;
                                        bestMatch = variable;
                                    }
                                }

                                return bestMatch;
                            }

                            private static int LevenshteinDistance(string s1, string s2)
                            {
                                int[,] d = new int[s1.Length + 1, s2.Length + 1];

                                for (int i = 0; i <= s1.Length; i++)
                                {
                                    d[i, 0] = i;
                                }

                                for (int j = 0; j <= s2.Length; j++)
                                {
                                    d[0, j] = j;
                                }

                                for (int i = 1; i <= s1.Length; i++)
                                {
                                    for (int j = 1; j <= s2.Length; j++)
                                    {
                                        int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                                        d[i, j] = Math.Min(
                                            Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                                            d[i - 1, j - 1] + cost);
                                    }
                                }

                                return d[s1.Length, s2.Length];
                            }
                        }

                        // PL007: Add missing closing $ to variable
                        internal class AddClosingDollarSignAction : CodeFixAction
                        {
                            public AddClosingDollarSignAction(ITextView textView, ITextBuffer textBuffer, ParseItem item)
                                : base(textView, textBuffer, item)
                            {
                            }

                            public override string DisplayText => "Add closing $";

                            public override void Invoke(CancellationToken cancellationToken)
                            {
                                var text = _item.Text.TrimEnd();

                                if (!text.EndsWith("$"))
                                {
                                    var endPosition = _item.Span.Start + text.Length;
                                    _textBuffer.Insert(endPosition, "$");
                                }
                            }
                        }

                        // Refactoring: Sort properties alphabetically with @ at top
                        internal class SortPropertiesAction : CodeFixAction
                        {
                            private readonly Entry _entry;

                            public SortPropertiesAction(ITextView textView, ITextBuffer textBuffer, Entry entry)
                                : base(textView, textBuffer, entry.RegistryKey)
                            {
                                _entry = entry;
                            }

                            public override string DisplayText => "Sort properties (@ first, then alphabetically)";

                            public override void Invoke(CancellationToken cancellationToken)
                            {
                                if (_entry.Properties.Count < 2)
                                {
                                    return;
                                }

                                // Sort properties: @ first, then alphabetically by name
                                var sortedProperties = _entry.Properties
                                    .OrderBy(p => p.Name.Text.Trim() == "@" ? 0 : 1)
                                    .ThenBy(p => p.Name.Text.Trim(), StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                // Check if already sorted
                                bool alreadySorted = true;
                                for (int i = 0; i < _entry.Properties.Count; i++)
                                {
                                    if (_entry.Properties[i] != sortedProperties[i])
                                    {
                                        alreadySorted = false;
                                        break;
                                    }
                                }

                                if (alreadySorted)
                                {
                                    return;
                                }

                                // Build the new text for the entry
                                var sb = new System.Text.StringBuilder();
                                sb.AppendLine(_entry.RegistryKey.Text.Trim());

                                foreach (var property in sortedProperties)
                                {
                                    sb.AppendLine($"{property.Name.Text.Trim()}={property.Value.Text.Trim()}");
                                }

                                // Replace the entire entry span
                                var span = new Span(_entry.Span.Start, _entry.Span.Length);
                                _textBuffer.Replace(span, sb.ToString().TrimEnd());
                            }
                        }

                            // PL008: Consolidate duplicate registry keys
                            internal class ConsolidateDuplicateKeysAction : CodeFixAction
                            {
                                private readonly Document _document;

                                public ConsolidateDuplicateKeysAction(ITextView textView, ITextBuffer textBuffer, ParseItem item, Document document)
                                    : base(textView, textBuffer, item)
                                {
                                    _document = document;
                                }

                                public override string DisplayText => "Consolidate duplicate registry keys";

                                public override void Invoke(CancellationToken cancellationToken)
                                {
                                    var duplicateKeyText = _item.Text.Trim();

                                    // Find all Entry items with the same registry key
                                    var allEntries = _document.Items
                                        .OfType<Entry>()
                                        .Where(e => e.RegistryKey.Text.Trim().Equals(duplicateKeyText, StringComparison.OrdinalIgnoreCase))
                                        .ToList();

                                    if (allEntries.Count < 2)
                                    {
                                        return;
                                    }

                                    // The first entry is the one we keep and merge into
                                    var firstEntry = allEntries[0];
                                    var duplicateEntries = allEntries.Skip(1).ToList();

                                    // Collect all properties from duplicate entries
                                    var propertiesToAdd = new List<string>();
                                    foreach (var entry in duplicateEntries)
                                    {
                                        foreach (var property in entry.Properties)
                                        {
                                            propertiesToAdd.Add($"{property.Name.Text.Trim()}={property.Value.Text.Trim()}");
                                        }
                                    }

                                    // Build the text to insert after the first entry's last property (or registry key if no properties)
                                    var insertText = new StringBuilder();
                                    foreach (var prop in propertiesToAdd)
                                    {
                                        insertText.AppendLine(prop);
                                    }

                                    // Apply changes in reverse order to preserve positions
                                    // First, delete duplicate entries from bottom to top
                                    var sortedDuplicates = duplicateEntries.OrderByDescending(e => e.Span.Start).ToList();

                                    using (var edit = _textBuffer.CreateEdit())
                                    {
                                        // Delete duplicate entries
                                        foreach (var entry in sortedDuplicates)
                                        {
                                            // Include any trailing newlines in the deletion
                                            var deleteStart = entry.Span.Start;
                                            var deleteEnd = entry.Span.End;

                                            // Extend to include the leading newline if there is one
                                            var snapshot = _textBuffer.CurrentSnapshot;
                                            if (deleteStart > 0)
                                            {
                                                var lineNumber = snapshot.GetLineNumberFromPosition(deleteStart);
                                                var line = snapshot.GetLineFromLineNumber(lineNumber);
                                                deleteStart = line.Start.Position;
                                            }

                                            // Extend to include trailing newlines
                                            while (deleteEnd < snapshot.Length)
                                            {
                                                char c = snapshot.GetText(deleteEnd, 1)[0];
                                                if (c == '\r' || c == '\n')
                                                {
                                                    deleteEnd++;
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                            edit.Delete(deleteStart, deleteEnd - deleteStart);
                                        }

                                        // Insert consolidated properties after the first entry
                                        if (propertiesToAdd.Count > 0)
                                        {
                                            var insertPosition = firstEntry.Span.End;
                                            edit.Insert(insertPosition, "\r\n" + insertText.ToString().TrimEnd());
                                        }

                                        edit.Apply();
                                    }
                                }
                            }

                                // Refactoring: Add default value @="" to registry key
                                internal class AddDefaultValueAction : CodeFixAction
                                {
                                private readonly Entry _entry;

                                public AddDefaultValueAction(ITextView textView, ITextBuffer textBuffer, Entry entry)
                                    : base(textView, textBuffer, entry.RegistryKey)
                                {
                                    _entry = entry;
                                }

                                public override string DisplayText => "Add default value @=\"\"";

                                public override void Invoke(CancellationToken cancellationToken)
                                {
                                    // Check if @ already exists
                                    bool hasDefaultValue = _entry.Properties.Any(p => p.Name.Text.Trim() == "@");
                                    if (hasDefaultValue)
                                    {
                                        return;
                                    }

                                    // Find the end of the registry key line (before any trailing newline)
                                    var keyText = _entry.RegistryKey.Text;
                                    var trimmedKeyText = keyText.TrimEnd('\r', '\n');
                                    var insertPosition = _entry.RegistryKey.Span.Start + trimmedKeyText.Length;

                                    // Always add newline before @="" and after it to push existing content down
                                    var textToInsert = _entry.Properties.Any() 
                                        ? "\r\n@=\"\"" 
                                        : "\r\n@=\"\"\r\n";

                                    _textBuffer.Insert(insertPosition, textToInsert);
                                }
                            }
                        }
