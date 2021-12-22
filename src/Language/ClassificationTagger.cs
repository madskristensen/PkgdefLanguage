using System;
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

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new ClassificationTagger(buffer, _classificationRegistry)) as ITagger<T>;
    }

    internal class ClassificationTagger : ITagger<IClassificationTag>
    {
        private readonly PkgdefDocument _document;
        private readonly ITextBuffer _buffer;
        private static Dictionary<ItemType, ClassificationTag> _map;

        internal ClassificationTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _document = buffer.GetDocument();
            _buffer = buffer;

            _map ??= new()
            {
                { ItemType.RegistryKey, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolDefinition)) },
                { ItemType.String, new ClassificationTag(registry.GetClassificationType(TypeNames.String)) },
                { ItemType.Literal, new ClassificationTag(registry.GetClassificationType(TypeNames.Literal)) },
                { ItemType.Comment, new ClassificationTag(registry.GetClassificationType(TypeNames.Comment)) },
                { ItemType.ReferenceBraces, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolDefinition)) },
                { ItemType.ReferenceName, new ClassificationTag(registry.GetClassificationType(TypeNames.SymbolReference)) },
            };
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (ParseItem item in _document.ItemsIntersectingWith(spans))
            {
                if (_map.ContainsKey(item.Type) && item.Span.End <= _buffer.CurrentSnapshot.Length)
                {
                    var itemSpan = new SnapshotSpan(_buffer.CurrentSnapshot, item);
                    yield return new TagSpan<IClassificationTag>(itemSpan, _map[item.Type]);

                    foreach (Reference variable in item.References)
                    {
                        var openSpan = new SnapshotSpan(_buffer.CurrentSnapshot, variable.Open);
                        yield return new TagSpan<IClassificationTag>(openSpan, _map[variable.Open.Type]);

                        var valueSpan = new SnapshotSpan(_buffer.CurrentSnapshot, variable.Value);
                        yield return new TagSpan<IClassificationTag>(valueSpan, _map[variable.Value.Type]);

                        var closeSpan = new SnapshotSpan(_buffer.CurrentSnapshot, variable.Close);
                        yield return new TagSpan<IClassificationTag>(closeSpan, _map[variable.Close.Type]);
                    }
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }
    }
}
