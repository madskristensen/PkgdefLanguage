using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
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

        public QuickInfoSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (triggerPoint.HasValue)
            {
                Document doc = PkgdefDocument.FromTextbuffer(_buffer);
                var position = triggerPoint.Value.Position;

                ParseItem token = doc.GetTokenFromPosition(position);

                if (token?.Type == ItemType.ReferenceName && CompletionCatalog.Variables.TryGetValue(token.Text, out var desc))
                {
                    ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
                    ITrackingSpan lineSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);

                    return Task.FromResult(new QuickInfoItem(lineSpan, desc));
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
