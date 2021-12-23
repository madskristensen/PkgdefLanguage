using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class RestErrorTaggerProvider : ITaggerProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<LexTag> tagAggregator = _bufferTagAggregator.CreateTagAggregator<LexTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(tagAggregator)) as ITagger<T>;
        }
    }

    public class ErrorTagger : LexTaggerConsumerBase<IErrorTag>
    {
        public ErrorTagger(ITagAggregator<LexTag> lexTags) : base(lexTags)
        { }

        public override IEnumerable<ITagSpan<IErrorTag>> GetTags(IMappingTagSpan<LexTag> span)
        {
            if (span.Tag.Item.IsValid)
            {
                yield break;
            }

            NormalizedSnapshotSpanCollection tagSpans = span.Span.GetSpans(span.Span.AnchorBuffer.CurrentSnapshot);

            foreach (SnapshotSpan tagSpan in tagSpans)
            {
                var errorTag = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, null);
                yield return new TagSpan<IErrorTag>(tagSpan, errorTag);
            }
        }
    }
}
