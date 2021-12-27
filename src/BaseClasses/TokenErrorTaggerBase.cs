using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public abstract class TokenErrorTaggerBase : ITaggerProvider
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

        public override IEnumerable<ITagSpan<IErrorTag>> GetTags(IMappingTagSpan<TokenTag> tagSpan)
        {
            if (tagSpan.Tag.IsValid)
            {
                yield break;
            }

            NormalizedSnapshotSpanCollection spans = tagSpan.Span.GetSpans(tagSpan.Span.AnchorBuffer.CurrentSnapshot);
            var tooltip = string.Join(Environment.NewLine, tagSpan.Tag.Errors);
            var errorTag = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, tooltip);

            foreach (SnapshotSpan span in spans)
            {
                yield return new TagSpan<IErrorTag>(span, errorTag);
            }
        }
    }
}
