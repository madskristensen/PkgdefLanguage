using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    public abstract class TokenTaggerConsumerBase<TTag> : ITagger<TTag>, IDisposable where TTag : ITag
    {
        private readonly ITagAggregator<TokenTag> _tags;
        private bool _isDisposed;

        public TokenTaggerConsumerBase(ITagAggregator<TokenTag> tags)
        {
            _tags = tags;
            _tags.TagsChanged += TokenTagsChanged;
        }

        private void TokenTagsChanged(object sender, TagsChangedEventArgs e)
        {
            foreach (SnapshotSpan span in e.Span.GetSpans(e.Span.AnchorBuffer))
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            }
        }

        public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            List<ITagSpan<TTag>> list = new();

            if (!spans[0].IsEmpty)
            {
                foreach (IMappingTagSpan<TokenTag> tagSpan in _tags.GetTags(spans))
                {
                    list.AddRange(GetTags(tagSpan));
                }
            }

            return list;
        }

        public abstract IEnumerable<ITagSpan<TTag>> GetTags(IMappingTagSpan<TokenTag> span);

        public virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _tags.TagsChanged -= TokenTagsChanged;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
