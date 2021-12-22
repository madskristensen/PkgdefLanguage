using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class PkgdefDocument : Document, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private bool _isDisposed;

        public PkgdefDocument(ITextBuffer buffer)
            : base(buffer.CurrentSnapshot.Lines.Select(line => line.GetTextIncludingLineBreak()).ToArray())
        {
            _buffer = buffer;
            _buffer.Changed += BufferChanged;
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            UpdateLines(_buffer.CurrentSnapshot.Lines.Select(line => line.GetTextIncludingLineBreak()).ToArray());
            ProcessAsync().FireAndForget();
        }

        public IEnumerable<ParseItem> ItemsIntersectingWith(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in spans)
            {
                if (span.IsEmpty || span.Length <= 2) // line breaks are usually 2 characters
                {
                    continue;
                }

                foreach (ParseItem item in Items.Where(i => i.Span.IntersectsWith(span)))
                {
                    yield return item;
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _buffer.Changed -= BufferChanged;
                _isDisposed = true;
            }
        }
    }

    public static class PkgdefDocumentExtensions
    {
        public static PkgdefDocument GetDocument(this ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new PkgdefDocument(buffer));
        }
    }
}