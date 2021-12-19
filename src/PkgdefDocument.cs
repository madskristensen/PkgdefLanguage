using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class PkgdefDocument : Document
    {
        private readonly ITextBuffer _buffer;

        public PkgdefDocument(ITextBuffer buffer)
            : base(buffer.CurrentSnapshot.Lines.Select(line => line.GetTextIncludingLineBreak()).ToArray())
        {
            _buffer = buffer;
            _buffer.Changed += BufferChanged;
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            UpdateLines(_buffer.CurrentSnapshot.Lines.Select(line => line.GetTextIncludingLineBreak()).ToArray());
            ParseAsync().FireAndForget();
        }

        public static PkgdefDocument FromTextbuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new PkgdefDocument(buffer));
        }
    }
}
