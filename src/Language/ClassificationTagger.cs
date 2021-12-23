using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using TypeNames = Microsoft.VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class ClassificationTaggerProvider : ITaggerProvider
    {
        [Import] internal IClassificationTypeRegistryService _classificationRegistry = null;
        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITagAggregator<LexTag> tagAggregator = _bufferTagAggregator.CreateTagAggregator<LexTag>(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(() => new ClassificationTagger(_classificationRegistry, tagAggregator)) as ITagger<T>;
        }
    }

    internal class ClassificationTagger : LexTaggerConsumerBase<IClassificationTag>
    {
        private static Dictionary<ItemType, ClassificationTag> _map;

        internal ClassificationTagger(IClassificationTypeRegistryService registry, ITagAggregator<LexTag> lexTags) : base(lexTags)
        {
            _map ??= new()
            {
                { ItemType.RegistryKey, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolDefinition)) },
                { ItemType.String, new ClassificationTag(registry.GetClassificationType(TypeNames.String)) },
                { ItemType.Literal, new ClassificationTag(registry.GetClassificationType(TypeNames.Literal)) },
                { ItemType.Comment, new ClassificationTag(registry.GetClassificationType(TypeNames.Comment)) },
                { ItemType.ReferenceBraces, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolDefinition)) },
                { ItemType.ReferenceName, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolReference)) },
                { ItemType.Operator, new ClassificationTag(registry.GetClassificationType(TypeNames.Operator)) },
            };
        }

        public override IEnumerable<ITagSpan<IClassificationTag>> GetTags(IMappingTagSpan<LexTag> span)
        {
            if (_map.TryGetValue(span.Tag.Item.Type, out ClassificationTag classificationTag))
            {
                NormalizedSnapshotSpanCollection tagSpans = span.Span.GetSpans(span.Span.AnchorBuffer.CurrentSnapshot);

                foreach (SnapshotSpan tagSpan in tagSpans)
                {
                    yield return new TagSpan<ClassificationTag>(tagSpan, classificationTag);
                }
            }
        }
    }
}
