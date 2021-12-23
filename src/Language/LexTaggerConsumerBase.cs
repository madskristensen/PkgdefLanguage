using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace PkgdefLanguage
{
    public abstract class LexTaggerConsumerBase<TTag, TLexTag> : ITagger<TTag>, IDisposable where TTag : ITag where TLexTag : ITag
    {
        private readonly ITagAggregator<TLexTag> _lexTags;
        private bool _isDisposed;

        public LexTaggerConsumerBase(ITagAggregator<TLexTag> lexTags)
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

            foreach (IMappingTagSpan<TLexTag> tagSpan in _lexTags.GetTags(spans))
            {
                list.AddRange(GetTags(tagSpan));
            }

            return list;
        }

        public abstract IEnumerable<ITagSpan<TTag>> GetTags(IMappingTagSpan<TLexTag> span);

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
