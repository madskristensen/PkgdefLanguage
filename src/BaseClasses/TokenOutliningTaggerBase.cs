using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public abstract class TokenOutliningTaggerBase : ITaggerProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<TokenTag> tags = _bufferTagAggregator.CreateTagAggregator<TokenTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new StructureTagger(tags)) as ITagger<T>;
        }
    }

    internal class StructureTagger : TokenTaggerConsumerBase<IStructureTag>
    {
        public StructureTagger(ITagAggregator<TokenTag> tags) : base(tags)
        { }

        public override IEnumerable<ITagSpan<IStructureTag>> GetTags(IMappingTagSpan<TokenTag> span)
        {
            if (!span.Tag.SupportOutlining)
            {
                yield break;
            }

            NormalizedSnapshotSpanCollection tagSpans = span.Span.GetSpans(span.Span.AnchorBuffer.CurrentSnapshot);

            foreach (SnapshotSpan tagSpan in tagSpans)
            {
                yield return CreateTag(tagSpan, tagSpan.GetText().Trim());
            }
        }

        private static TagSpan<IStructureTag> CreateTag(SnapshotSpan span, string text)
        {
            var structureTag = new StructureTag(
                        span.Snapshot,
                        outliningSpan: span,
                        guideLineSpan: span,
                        guideLineHorizontalAnchor: span.Start,
                        type: PredefinedStructureTagTypes.Structural,
                        isCollapsible: true,
                        collapsedForm: text,
                        collapsedHintForm: null);

            return new TagSpan<IStructureTag>(span, structureTag);
        }
    }
}