using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace PkgdefLanguage
{
    public abstract class LexTaggerConsumerBase<TTag, TLex> : ITagger<TTag>, IDisposable where TTag : ITag where TLex : ITag
    {
        private readonly ITagAggregator<TLex> _lexTags;
        private bool _isDisposed;

        public LexTaggerConsumerBase(ITagAggregator<TLex> lexTags)
        {
            _lexTags = lexTags;
            _lexTags.TagsChanged += LexTagsChanged;
        }

        private void LexTagsChanged(object sender, TagsChangedEventArgs e)
        {
            foreach (SnapshotSpan span in e.Span.GetSpans(e.Span.AnchorBuffer))
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            }
        }

        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            List<ITagSpan<TTag>> list = new();

            foreach (IMappingTagSpan<TLex> tagSpan in _lexTags.GetTags(spans))
            {
                list.AddRange(GetTags(tagSpan));
            }

            return list;
        }

        public abstract IEnumerable<ITagSpan<TTag>> GetTags(IMappingTagSpan<TLex> span);

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _lexTags.TagsChanged -= LexTagsChanged;
            }

            _isDisposed = true;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
