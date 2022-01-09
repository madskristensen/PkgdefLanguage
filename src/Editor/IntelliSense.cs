//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.ComponentModel.Composition;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.Core.Imaging;
//using Microsoft.VisualStudio.Imaging;
//using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
//using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Adornments;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Utilities;
//using Microsoft.Win32;

//namespace PkgdefLanguage
//{
//    [Export(typeof(IAsyncCompletionSourceProvider))]
//    [ContentType(Constants.LanguageName)]
//    [Name(Constants.LanguageName)]
//    public class IntelliSense : IAsyncCompletionSourceProvider
//    {
//        public IAsyncCompletionSource GetOrCreate(ITextView textView) =>
//            textView.Properties.GetOrCreateSingletonProperty(() => new AsyncCompletionSource());
//    }

//    public class AsyncCompletionSource : IAsyncCompletionSource
//    {
//        private static readonly ImageElement _referenceIcon = new(KnownMonikers.LocalVariable.ToImageId(), "Variable");

//        public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken cancellationToken)
//        {
//            Document document = session.TextView.TextBuffer.GetDocument();
//            ParseItem item = document.FindItemFromPosition(triggerLocation.Position);
//            IEnumerable<CompletionItem> items = null;

//            if (item?.Type == ItemType.Reference)
//            {
//                items = GetReferenceCompletion();
//            }
//            else if (item?.Type == ItemType.RegistryKey)
//            {
//                items = GetRegistryKeyCompletion(item, triggerLocation);
//            }

//            return Task.FromResult(items == null ? null : new CompletionContext(items.ToImmutableArray()));
//        }

//        private IEnumerable<CompletionItem> GetRegistryKeyCompletion(ParseItem item, SnapshotPoint triggerLocation)
//        {
//            ITextSnapshotLine line = triggerLocation.GetContainingLine();
//            var column = triggerLocation.Position - line.Start - 1;
//            var previousKey = item.Text.LastIndexOf('\\', column);

//            if (previousKey > -1)
//            {
//                IEnumerable<string> prevKeys = item.Text.Substring(0, previousKey).Split('\\').Skip(1);
//                RegistryKey root = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration);
//                RegistryKey parent = root;

//                foreach (var subKey in prevKeys)
//                {
//                    parent = parent.OpenSubKey(subKey);
//                }

//                return parent?.GetSubKeyNames()?.Select(s => new CompletionItem(s, this, _referenceIcon));
//            }

//            return null;
//        }

//        private IEnumerable<CompletionItem> GetReferenceCompletion()
//        {
//            foreach (var key in PredefinedVariables.Variables.Keys)
//            {
//                var completion = new CompletionItem(key, this, _referenceIcon, ImmutableArray<CompletionFilter>.Empty, "", $"${key}$", key, key, ImmutableArray<ImageElement>.Empty);
//                completion.Properties.AddProperty("description", PredefinedVariables.Variables[key]);
//                yield return completion;
//            }
//        }

//        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
//        {
//            if (item.Properties.TryGetProperty("description", out string description))
//            {
//                return Task.FromResult<object>(description);
//            }

//            return Task.FromResult<object>(null);
//        }

//        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
//        {
//            if (trigger.Character == '\n' || trigger.Character == ']' || trigger.Reason == CompletionTriggerReason.Deletion)
//            {
//                return CompletionStartData.DoesNotParticipateInCompletion;
//            }

//            Document document = triggerLocation.Snapshot.TextBuffer.GetDocument();
//            ParseItem item = document?.FindItemFromPosition(triggerLocation.Position);

//            if (item?.Type == ItemType.Reference)
//            {
//                var tokenSpan = new SnapshotSpan(triggerLocation.Snapshot, item);
//                return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
//            }
//            else if (item?.Type == ItemType.RegistryKey && item.Text.IndexOf("$rootkey$", StringComparison.OrdinalIgnoreCase) > -1)
//            {
//                var column = triggerLocation.Position - item.Span.Start;

//                if (column < 1)
//                {
//                    return CompletionStartData.DoesNotParticipateInCompletion; ;
//                }

//                var start = item.Text.LastIndexOf('\\', column - 1) + 1;
//                var end = item.Text.IndexOf('\\', column);
//                var close = item.Text.IndexOf(']', column);
//                var textEnd = item.Text.IndexOf(']', column);
//                end = end >= start ? end : close;
//                end = end >= start ? end : textEnd;
//                end = end >= start ? end : item.Text.TrimEnd().Length;

//                if (end >= start)
//                {
//                    if (close == -1 || column <= close)
//                    {
//                        var span = new SnapshotSpan(triggerLocation.Snapshot, item.Span.Start + start, end - start);
//                        return new CompletionStartData(CompletionParticipation.ProvidesItems, span);
//                    }
//                }
//            }

//            return CompletionStartData.DoesNotParticipateInCompletion;
//        }
//    }
//}