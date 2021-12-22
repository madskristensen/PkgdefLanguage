using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class StructureTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
            buffer.Properties.GetOrCreateSingletonProperty(() => new StructureTagger(buffer)) as ITagger<T>;
    }

    public class StructureTagger : ITagger<IStructureTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly PkgdefDocument _document;
        private List<ITagSpan<IStructureTag>> _structureTags = new();
        private bool _isDisposed;

        public StructureTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = PkgdefDocument.FromTextbuffer(buffer);
            _document.Parsed += DocumentParsed;

            StartParsing();
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            StartParsing();
        }

        public IEnumerable<ITagSpan<IStructureTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || spans[0].IsEmpty || !_structureTags.Any() || spans[0].Snapshot != _buffer.CurrentSnapshot)
            {
                return null;
            }

            return _structureTags;
        }

        private void StartParsing()
        {
            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                if (TagsChanged != null && !_document.IsParsing)
                {
                    ReParse();
                    SnapshotSpan span = new(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
                }

                return Task.CompletedTask;
            },
                VsTaskRunContext.UIThreadBackgroundPriority).FireAndForget();
        }

        private void ReParse()
        {
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            List<ITagSpan<IStructureTag>> list = new();

            foreach (Entry entry in _document.Entries.Where(r => r.Properties.Any()))
            {
                var text = entry.RegistryKey.Text.Trim();
                var tooltip = entry.ToString();

                var simpleSpan = new Span(entry.Start, entry.Length);
                var snapShotSpan = new SnapshotSpan(snapshot, simpleSpan);
                TagSpan<IStructureTag> tag = CreateTag(snapShotSpan, text, tooltip);
                list.Add(tag);
            }

            _structureTags = list;
        }

        private static TagSpan<IStructureTag> CreateTag(SnapshotSpan span, string text, string tooltip)
        {
            var structureTag = new StructureTag(
                        span.Snapshot,
                        outliningSpan: span,
                        guideLineSpan: span,
                        guideLineHorizontalAnchor: span.Start,
                        type: PredefinedStructureTagTypes.Structural,
                        isCollapsible: true,
                        collapsedForm: text,
                        collapsedHintForm: tooltip);

            return new TagSpan<IStructureTag>(span, structureTag);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _document.Parsed -= DocumentParsed;
            }

            _isDisposed = true;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
