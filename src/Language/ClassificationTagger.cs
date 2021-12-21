using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal class ClassificationTaggerProvider : ITaggerProvider
    {
        [Import] internal IClassificationTypeRegistryService _classificationRegistry = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new ClassificationTagger(buffer, _classificationRegistry)) as ITagger<T>;
    }

    internal class ClassificationTagger : ITagger<IClassificationTag>
    {
        private readonly PkgdefDocument _document;
        private static Dictionary<ItemType, IClassificationType> _map;

        internal ClassificationTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _document = PkgdefDocument.FromTextbuffer(buffer);

            _map ??= new Dictionary<ItemType, IClassificationType> {
                { ItemType.RegistryKey, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition) },
                { ItemType.String, registry.GetClassificationType(PredefinedClassificationTypeNames.String) },
                { ItemType.Literal, registry.GetClassificationType(PredefinedClassificationTypeNames.Literal)},
                { ItemType.Comment, registry.GetClassificationType(PredefinedClassificationTypeNames.Comment)},
                { ItemType.ReferenceBraces, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition)},
                { ItemType.ReferenceName, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolReference)},
            };
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in spans.Where(s => !s.IsEmpty))
            {
                foreach (ParseItem item in _document.Items.Where(t => t.Start < span.End && t.End > span.Start))
                {
                    if (_map.ContainsKey(item.Type) && item.End <= span.Snapshot.Length)
                    {
                        var itemSpan = new SnapshotSpan(span.Snapshot, item.Start, item.Length);
                        var itemTag = new ClassificationTag(_map[item.Type]);
                        yield return new TagSpan<IClassificationTag>(itemSpan, itemTag);

                        foreach (Reference variable in item.References)
                        {
                            var openSpan = new SnapshotSpan(span.Snapshot, variable.Open.Start, variable.Open.Length);
                            var openTag = new ClassificationTag(_map[variable.Open.Type]);
                            yield return new TagSpan<IClassificationTag>(openSpan, openTag);

                            var valueSpan = new SnapshotSpan(span.Snapshot, variable.Value.Start, variable.Value.Length);
                            var valueTag = new ClassificationTag(_map[variable.Value.Type]);
                            yield return new TagSpan<IClassificationTag>(valueSpan, valueTag);

                            var closeSpan = new SnapshotSpan(span.Snapshot, variable.Close.Start, variable.Close.Length);
                            var closeTag = new ClassificationTag(_map[variable.Close.Type]);
                            yield return new TagSpan<IClassificationTag>(closeSpan, closeTag);
                        }
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
