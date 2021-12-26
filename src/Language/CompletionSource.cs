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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        [Import] internal ITextStructureNavigatorSelectorService _structureNavigator = null;

        public IAsyncCompletionSource GetOrCreate(ITextView textView) =>
            textView.Properties.GetOrCreateSingletonProperty(() => new CompletionSource(_structureNavigator));
    }

    public class CompletionSource : IAsyncCompletionSource
    {
        private readonly ITextStructureNavigatorSelectorService _structureNavigator;
        private static readonly ImageElement _referenceIcon = new(KnownMonikers.LocalVariable.ToImageId(), "Variable");

        public CompletionSource(ITextStructureNavigatorSelectorService structureNavigator)
        {
            _structureNavigator = structureNavigator;
        }

        public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken cancellationToken)
        {
            Document document = session.TextView.TextBuffer.GetDocument();
            ParseItem item = document.GetTokenFromPosition(triggerLocation.Position);

            if (item?.Type == ItemType.ReferenceName)
            {
                return Task.FromResult(GetCompletionItems());
            }

            return Task.FromResult<CompletionContext>(null);
        }

        /// <summary>
        /// Returns completion items applicable to the value portion of the key-value pair
        /// </summary>
        private CompletionContext GetCompletionItems()
        {
            List<CompletionItem> items = new();

            foreach (var key in PredefinedVariables.Variables.Keys)
            {
                var completion = new CompletionItem(key, this, _referenceIcon, ImmutableArray<CompletionFilter>.Empty, "", $"${key}$", key, key, ImmutableArray<ImageElement>.Empty);
                completion.Properties.AddProperty("description", PredefinedVariables.Variables[key]);
                items.Add(completion);
            }

            return new CompletionContext(items.ToImmutableArray());
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
            // We don't trigger completion when user typed
            if (char.IsNumber(trigger.Character)         // a number
                || char.IsPunctuation(trigger.Character) // punctuation
                || char.IsSymbol(trigger.Character)      // punctuation
                || trigger.Character == '\n'             // new line
                || trigger.Reason == CompletionTriggerReason.Backspace
                || trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // We participate in completion and provide the "applicable to span".
            // This span is used:
            // 1. To search (filter) the list of all completion items
            // 2. To highlight (bold) the matching part of the completion items
            // 3. In standard cases, it is replaced by content of completion item upon commit.

            // If you want to extend a language which already has completion, don't provide a span, e.g.
            // return CompletionStartData.ParticipatesInCompletionIfAny

            // If you provide a language, but don't have any items available at this location,
            // consider providing a span for extenders who can't parse the codem e.g.
            // return CompletionStartData(CompletionParticipation.DoesNotProvideItems, spanForOtherExtensions);

            SnapshotSpan tokenSpan = FindTokenSpanAtPosition(triggerLocation);

            foreach (var commentChar in Constants.CommentChars)
            {
                if (triggerLocation.GetContainingLine().GetText().StartsWith(commentChar, StringComparison.Ordinal))
                {
                    return CompletionStartData.DoesNotParticipateInCompletion;
                }
            }

            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }

        private SnapshotSpan FindTokenSpanAtPosition(SnapshotPoint triggerLocation)
        {
            // This method is not really related to completion,
            // we mostly work with the default implementation of ITextStructureNavigator 
            // You will likely use the parser of your language
            ITextStructureNavigator navigator = _structureNavigator.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            TextExtent extent = navigator.GetExtentOfWord(triggerLocation);
            if (triggerLocation.Position > 0 && (!extent.IsSignificant || !extent.Span.GetText().Any(c => char.IsLetterOrDigit(c))))
            {
                // Improves span detection over the default ITextStructureNavigation result
                extent = navigator.GetExtentOfWord(triggerLocation - 1);
            }

            ITrackingSpan tokenSpan = triggerLocation.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);

            ITextSnapshot snapshot = triggerLocation.Snapshot;
            var tokenText = tokenSpan.GetText(snapshot);
            if (string.IsNullOrWhiteSpace(tokenText))
            {
                // The token at this location is empty. Return an empty span, which will grow as user types.
                return new SnapshotSpan(triggerLocation, 0);
            }

            // Trim quotes and new line characters.
            var startOffset = 0;
            var endOffset = 0;

            if (tokenText.Length > 0)
            {
                if (tokenText.StartsWith("\""))
                {
                    startOffset = 1;
                }
            }
            if (tokenText.Length - startOffset > 0)
            {
                if (tokenText.EndsWith("\"\r\n"))
                {
                    endOffset = 3;
                }
                else if (tokenText.EndsWith("\r\n"))
                {
                    endOffset = 2;
                }
                else if (tokenText.EndsWith("\"\n"))
                {
                    endOffset = 2;
                }
                else if (tokenText.EndsWith("\n"))
                {
                    endOffset = 1;
                }
                else if (tokenText.EndsWith("\""))
                {
                    endOffset = 1;
                }
            }

            return new SnapshotSpan(tokenSpan.GetStartPoint(snapshot) + startOffset, tokenSpan.GetEndPoint(snapshot) - endOffset);
        }
    }
}
