using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace BaseClasses
{
    internal abstract class TokenErrorListBase : WpfTextViewCreationListener
    {
        private ITextBuffer _buffer;
        private TableDataSource _dataSource;
        private ITagAggregator<TokenTag> _tags;

        [Import] internal IBufferTagAggregatorFactoryService _bufferTagAggregator = null;

        protected override void Created(DocumentView docView)
        {
            _buffer = docView.TextBuffer;
            _dataSource = new TableDataSource(_buffer.ContentType.DisplayName);

            _tags = _bufferTagAggregator.CreateTagAggregator<TokenTag>(_buffer);
            _tags.TagsChanged += OnTokenTagsChanged;
        }

        private void OnTokenTagsChanged(object sender = null, TagsChangedEventArgs e = null)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            NormalizedSnapshotSpanCollection spans = new(span);

            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                IEnumerable<IMappingTagSpan<TokenTag>> errorTags = _tags.GetTags(spans).Where(t => !t.Tag.IsValid);

                if (!errorTags.Any())
                {
                    _dataSource.CleanAllErrors();
                }
                else
                {
                    IEnumerable<ErrorListItem> errors = errorTags.SelectMany(e => e.Tag.Errors);
                    _dataSource.AddErrors(errors);
                }
            });
        }

        protected override void Closed(IWpfTextView textView)
        {
            _tags.TagsChanged -= OnTokenTagsChanged;
            _dataSource.CleanAllErrors();
        }
    }
}

