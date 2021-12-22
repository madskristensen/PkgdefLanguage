using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class ErrorListManager : WpfTextViewCreationListener
    {
        private DocumentView _docView;
        private Project _project;
        private TableDataSource _dataSource;
        private PkgdefDocument _document;

        protected override async Task CreatedAsync(DocumentView docView)
        {
            _docView = docView;
            _project = await VS.Solutions.GetActiveProjectAsync();
            _dataSource = new TableDataSource(Constants.LanguageName, Constants.LanguageName);
            _document = PkgdefDocument.FromTextbuffer(docView.TextBuffer);
            _document.Parsed += ParseErrors;

            ParseErrors();
        }

        private void ParseErrors(object sender = null, EventArgs e = null)
        {
            if (_document.IsValid)
            {
                _dataSource.CleanAllErrors();
                return;
            }

            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                if (_document.IsParsing)
                {
                    return;
                }

                List<ErrorListItem> errors = new();

                foreach (ParseItem item in _document.Items)
                {
                    if (!item.IsValid)
                    {
                        errors.AddRange(CreateErrorListItem(item));
                    }

                    foreach (Reference reference in item.References)
                    {
                        if (!reference.Value.IsValid)
                        {
                            errors.AddRange(CreateErrorListItem(reference.Value));
                        }
                    }
                }

                _dataSource.CleanAllErrors();
                _dataSource.AddErrors(_project?.Name ?? "", errors);
            }, VsTaskRunContext.UIThreadBackgroundPriority);
        }

        private IEnumerable<ErrorListItem> CreateErrorListItem(ParseItem item)
        {
            ITextSnapshotLine line = _docView.TextBuffer.CurrentSnapshot.GetLineFromPosition(item.Span.Start);

            foreach (var error in item.Errors)
            {
                yield return new ErrorListItem
                {
                    ProjectName = _project?.Name ?? "",
                    FileName = _docView.FilePath,
                    Message = error,
                    ErrorCategory = "syntax",
                    Severity = __VSERRORCATEGORY.EC_WARNING,
                    Line = line.LineNumber,
                    Column = item.Span.Start - line.Start.Position,
                    BuildTool = Vsix.Name,
                };
            }
        }

        protected override void Closed(IWpfTextView textView)
        {
            _document.Parsed -= ParseErrors;
            _document.Dispose();
            _dataSource.CleanAllErrors();
        }
    }
}
