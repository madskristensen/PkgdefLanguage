using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public abstract class TokenErrorBaseTagger : ITaggerProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<TokenTag> tags = _bufferTagAggregator.CreateTagAggregator<TokenTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(tags)) as ITagger<T>;
        }
    }

    public class ErrorTagger : TokenTaggerConsumerBase<IErrorTag>
    {
        public ErrorTagger(ITagAggregator<TokenTag> tags) : base(tags)
        { }

        public override IEnumerable<ITagSpan<IErrorTag>> GetTags(IMappingTagSpan<TokenTag> span)
        {
            if (span.Tag.IsValid)
            {
                yield break;
            }

            NormalizedSnapshotSpanCollection tagSpans = span.Span.GetSpans(span.Span.AnchorBuffer.CurrentSnapshot);
            var tooltip = string.Join(Environment.NewLine, span.Tag.ErrorMessages);
            var errorTag = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, tooltip);

            foreach (SnapshotSpan tagSpan in tagSpans)
            {
                yield return new TagSpan<IErrorTag>(tagSpan, errorTag);

            }
        }
    }
}
