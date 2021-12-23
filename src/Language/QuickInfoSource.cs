using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer buffer) =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new QuickInfoSource(buffer));
    }

    internal sealed class QuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _buffer;
        private readonly PkgdefDocument _document;
        private static readonly ImageId _errorIcon = KnownMonikers.StatusWarningNoColor.ToImageId();

        public QuickInfoSource(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = buffer.GetDocument();
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (triggerPoint.HasValue)
            {
                var position = triggerPoint.Value.Position;

                ParseItem item = _document.GetTokenFromPosition(position);

                if (item?.IsValid == false)
                {
                    ITrackingSpan span = _buffer.CurrentSnapshot.CreateTrackingSpan(item, SpanTrackingMode.EdgeInclusive);

                    var elm = new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ImageElement(_errorIcon),
                        string.Join(Environment.NewLine, item.Errors));

                    return Task.FromResult(new QuickInfoItem(span, elm));
                }
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        public void Dispose()
        {
            // This provider does not perform any cleanup.
        }
    }
}
