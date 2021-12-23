using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class StructureTaggerProvider : ITaggerProvider
    {
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<LexTag> lexTags = _bufferTagAggregator.CreateTagAggregator<LexTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new StructureTagger(lexTags)) as ITagger<T>;
        }
    }

    public class StructureTagger : LexTaggerConsumerBase<IStructureTag>
    {
        public StructureTagger(ITagAggregator<LexTag> lexTags) : base(lexTags)
        { }

        public override IEnumerable<ITagSpan<IStructureTag>> GetTags(IMappingTagSpan<LexTag> span)
        {
            if (span.Tag.Item is not Entry entry)
            {
                yield break;
            }

            NormalizedSnapshotSpanCollection tagSpans = span.Span.GetSpans(span.Span.AnchorBuffer.CurrentSnapshot);

            foreach (SnapshotSpan tagSpan in tagSpans)
            {
                yield return CreateTag(tagSpan, entry.RegistryKey.Text.Trim());
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
