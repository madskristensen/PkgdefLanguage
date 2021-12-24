using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using BaseClasses;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(TokenTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class TokenTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new TokenTagger(buffer)) as ITagger<T>;
    }

    internal class TokenTagger : ITagger<TokenTag>, IDisposable
    {
        private readonly PkgdefDocument _document;
        private readonly ITextBuffer _buffer;
        private Dictionary<ParseItem, ITagSpan<TokenTag>> _tagsCache;
        private bool _isDisposed;

        internal TokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = buffer.GetDocument();
            _document.Processed += ReParse;
            _tagsCache = new Dictionary<ParseItem, ITagSpan<TokenTag>>();
            ReParse();
        }

        public IEnumerable<ITagSpan<TokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _tagsCache.Values;
        }

        private void ReParse(object sender = null, EventArgs e = null)
        {
            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                Dictionary<ParseItem, ITagSpan<TokenTag>> list = new();

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

                _tagsCache = list;

                SnapshotSpan span = new(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));

            }, VsTaskRunContext.UIThreadIdlePriority);
        }

        private void AddTagToList(Dictionary<ParseItem, ITagSpan<TokenTag>> list, ParseItem item)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, item);
            var tag = new TagSpan<TokenTag>(span, new TokenTag(item.Type, item is Entry, item.Errors.Select(e => e.Message).ToArray()));
            list.Add(item, tag);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Processed -= ReParse;
                _document.Dispose();
            }

            _isDisposed = true;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
