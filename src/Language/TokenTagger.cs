﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
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

    internal class TokenTagger : ITagger<TokenTag>, IDisposable
    {
        private readonly Document _document;
        private readonly ITextBuffer _buffer;
        private Dictionary<ParseItem, ITagSpan<TokenTag>> _tagsCache;
        private static readonly ImageId _errorIcon = KnownMonikers.StatusWarningNoColor.ToImageId();
        private bool _isDisposed;

        internal TokenTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = buffer.GetDocument();
            _document.Processed += ReParse;
            _tagsCache = new Dictionary<ParseItem, ITagSpan<TokenTag>>();

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;
                ReParse();
            }).FireAndForget();
        }

        public IEnumerable<ITagSpan<TokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return _tagsCache.Values;
        }

        private void ReParse(object sender = null, EventArgs e = null)
        {
            // Make sure this is running on a background thread.
            ThreadHelper.ThrowIfOnUIThread();

            Dictionary<ParseItem, ITagSpan<TokenTag>> list = new();

            foreach (ParseItem item in _document.Items)
            {
                if (_document.IsProcessing)
                {
                    // Abort and wait for the next parse event to finish
                    return;
                }

                AddTagToList(list, item);

                foreach (ParseItem variable in item.References)
                {
                    AddTagToList(list, variable);
                }
            }

            _tagsCache = list;

            SnapshotSpan span = new(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        private void AddTagToList(Dictionary<ParseItem, ITagSpan<TokenTag>> list, ParseItem item)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, item);

            var tag = new TokenTag(
                tokenType: item.Type,
                supportOutlining: item is Entry entry && entry.Properties.Any(),
                getTooltipAsync: item.IsValid ? null : GetTooltipAsync,
                errors: CreateErrorListItem(item).ToArray());

            list.Add(item, new TagSpan<TokenTag>(span, tag));
        }

        private IEnumerable<ErrorListItem> CreateErrorListItem(ParseItem item)
        {
            ITextSnapshotLine line = _buffer.CurrentSnapshot.GetLineFromPosition(item.Span.Start);

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
                    ErrorCode = error.ErrorCode
                };
            }
        }

        private Task<object> GetTooltipAsync(SnapshotPoint triggerPoint)
        {
            ParseItem item = _document.FindItemFromPosition(triggerPoint.Position);

            // Error messages
            if (item?.IsValid == false)
            {
                var elm = new ContainerElement(
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
                _document.Processed -= ReParse;
                _document.Dispose();
            }

            _isDisposed = true;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
