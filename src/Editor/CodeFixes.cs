using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
                    e.ErrorCode == "PL005"));

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

            // Find all items with errors in this range (not just at a position)
            var itemsWithErrors = FindAllItemsWithErrorsInRange(document, range.Start.Position, range.End.Position);

            if (!itemsWithErrors.Any())
            {
                yield break;
            }

            var actions = new List<ISuggestedAction>();

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
                    }
                }
            }

            if (actions.Any())
            {
                yield return new SuggestedActionSet(null, actions, null, SuggestedActionSetPriority.Medium);
            }
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
        }
