using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class RestErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(buffer)) as ITagger<T>;
    }

    public class ErrorTagger : ITagger<IErrorTag>
    {
        private readonly PkgdefDocument _document;
        private readonly ITextBuffer _buffer;

        public ErrorTagger(ITextBuffer buffer)
        {
            _document = buffer.GetDocument();
            _buffer = buffer;
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_document.IsProcessing || _document.IsValid)
            {
                yield break;
            }

            foreach (ParseItem item in _document.ItemsIntersectingWith(spans))
            {
                if (!item.IsValid)
                {
                    var tooltip = string.Join(Environment.NewLine, item.Errors);

                    var snapShotSpan = new SnapshotSpan(_buffer.CurrentSnapshot, item);
                    var errorTag = new ErrorTag(PredefinedErrorTypeNames.SyntaxError, tooltip);

                    yield return new TagSpan<IErrorTag>(snapShotSpan, errorTag);
                }

                foreach (Reference reference in item.References)
                {
                    if (!reference.Value.IsValid)
                    {
                        var tooltip = string.Join(Environment.NewLine, reference.Value.Errors);

                        var snapShotSpan = new SnapshotSpan(_buffer.CurrentSnapshot, reference.Value);
                        var errorTag = new ErrorTag(PredefinedErrorTypeNames.CompilerError, tooltip);

                        yield return new TagSpan<IErrorTag>(snapShotSpan, errorTag);
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
