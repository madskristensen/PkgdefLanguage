using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(LexTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class LexTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new LexTagger(buffer)) as ITagger<T>;
    }

    public record LexTag(ParseItem Item) : ITag;

    internal class LexTagger : ITagger<LexTag>, IDisposable
    {
        private readonly PkgdefDocument _document;
        private readonly ITextBuffer _buffer;
        private Dictionary<ParseItem, ITagSpan<LexTag>> _tagsCache;
        private bool _isDisposed;

        internal LexTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = buffer.GetDocument();
            _document.Processed += ReParse;
            _tagsCache = new Dictionary<ParseItem, ITagSpan<LexTag>>();
            ReParse();
        }

        public IEnumerable<ITagSpan<LexTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _tagsCache.Values;
        }

        private void ReParse(object sender = null, EventArgs e = null)
        {
            if (_document.IsProcessing)
            {
                return;
            }

            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                if (_document.IsProcessing)
                {
                    return;
                }

                Dictionary<ParseItem, ITagSpan<LexTag>> list = new();

                foreach (ParseItem item in _document.Items)
                {
                    AddTagToList(list, item);

                    foreach (Reference variable in item.References)
                    {
                        AddTagToList(list, variable.Open);
                        AddTagToList(list, variable.Value);
                        AddTagToList(list, variable.Close);
                    }
                }

                UpdateCache(list);

            }, VsTaskRunContext.UIThreadBackgroundPriority);
        }

        private void UpdateCache(Dictionary<ParseItem, ITagSpan<LexTag>> list)
        {
            IEnumerable<ParseItem> diff = list.Keys.Except(_tagsCache.Keys);

            if (diff.Any())
            {
                _tagsCache = list;
                var start = diff.First().Span.Start;
                var length = diff.Last().Span.End - start;

                SnapshotSpan span = new(_buffer.CurrentSnapshot, start, length);
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            }
        }

        private void AddTagToList(Dictionary<ParseItem, ITagSpan<LexTag>> list, ParseItem item)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, item);
            var tag = new TagSpan<LexTag>(span, new LexTag(item));
            list.Add(item, tag);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Processed -= ReParse;
            }

            _isDisposed = true;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
