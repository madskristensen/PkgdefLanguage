using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
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

    internal class TokenTagger : TokenTaggerBase, IDisposable
    {
        private readonly Document _document;
        private static readonly ImageId _errorIcon = KnownMonikers.StatusWarningNoColor.ToImageId();
        private bool _isDisposed;

        internal TokenTagger(ITextBuffer buffer) : base(buffer)
        {
            _document = buffer.GetDocument();
            _document.Processed += DocumentProcessed;
        }

        private void DocumentProcessed(Document document)
        {
            _ = TokenizeAsync();
        }

        public override Task TokenizeAsync()
        {
            List<ITagSpan<TokenTag>> list = new();

            foreach (ParseItem item in _document.Items)
            {
                if (_document.IsProcessing)
                {
                    // Abort and wait for the next parse event to finish
                    return Task.CompletedTask;
                }

                ConvertItemToTag(list, item);

                foreach (ParseItem variable in item.References)
                {
                    ConvertItemToTag(list, variable);
                }
            }

            OnTagsUpdated(list);
            return Task.CompletedTask;
        }

        private void ConvertItemToTag(List<ITagSpan<TokenTag>> list, ParseItem item)
        {
            var hasTooltip = !item.IsValid;
            var supportsOutlining = item is Entry entry && entry.Properties.Any();
            IEnumerable<ErrorListItem> errors = CreateErrorListItems(item);

            TokenTag tag = CreateToken(item.Type, hasTooltip, supportsOutlining, errors);

            SnapshotSpan span = new(Buffer.CurrentSnapshot, item);
            list.Add(new TagSpan<TokenTag>(span, tag));
        }

        private IEnumerable<ErrorListItem> CreateErrorListItems(ParseItem item)
        {
            ITextSnapshotLine line = Buffer.CurrentSnapshot.GetLineFromPosition(item.Span.Start);

            foreach (Error error in item.Errors)
            {
                yield return new ErrorListItem
                {
                    ProjectName = _document.ProjectName ?? "",
                    FileName = _document.FileName,
                    Message = error.Message,
                    ErrorCategory = error.Category,
                    Severity = error.Severity,
                    Line = line.LineNumber,
                    Column = item.Span.Start - line.Start.Position,
                    BuildTool = Vsix.Name,
                    ErrorCode = error.ErrorCode,
                    HelpLink = error.HelpLink
                };
            }
        }

        public override Task<object> GetTooltipAsync(SnapshotPoint triggerPoint)
        {
            ParseItem item = _document.FindItemFromPosition(triggerPoint.Position);

            // Error messages
            if (item?.IsValid == false)
            {
                ContainerElement elm = new(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(_errorIcon),
                    string.Join(Environment.NewLine, item.Errors.Select(e => e.Message)));

                return Task.FromResult<object>(elm);
            }

            return Task.FromResult<object>(null);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Processed -= DocumentProcessed;
                _document.Dispose();
            }

            _isDisposed = true;
        }
    }
}
