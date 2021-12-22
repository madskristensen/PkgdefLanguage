using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
        private static Dictionary<ItemType, ClassificationTag> _map;

        internal ClassificationTagger(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _document = PkgdefDocument.FromTextbuffer(buffer);

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
            foreach (SnapshotSpan span in spans.Where(s => !s.IsEmpty))
            {
                foreach (ParseItem item in _document.Items.Where(t => t.Start < span.End && t.End > span.Start))
                {
                    if (_map.ContainsKey(item.Type) && item.End <= span.Snapshot.Length)
                    {
                        var itemSpan = new SnapshotSpan(span.Snapshot, item.Start, item.Length);
                        yield return new TagSpan<IClassificationTag>(itemSpan, _map[item.Type]);

                        foreach (Reference variable in item.References)
                        {
                            var openSpan = new SnapshotSpan(span.Snapshot, variable.Open.Start, variable.Open.Length);
                            yield return new TagSpan<IClassificationTag>(openSpan, _map[variable.Open.Type]);

                            var valueSpan = new SnapshotSpan(span.Snapshot, variable.Value.Start, variable.Value.Length);
                            yield return new TagSpan<IClassificationTag>(valueSpan, _map[variable.Value.Type]);

                            var closeSpan = new SnapshotSpan(span.Snapshot, variable.Close.Start, variable.Close.Length);
                            yield return new TagSpan<IClassificationTag>(closeSpan, _map[variable.Close.Type]);
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
