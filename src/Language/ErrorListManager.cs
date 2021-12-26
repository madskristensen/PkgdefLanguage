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
            _dataSource = new TableDataSource(Constants.LanguageName);

            _document = docView.TextBuffer.GetDocument();
            _document.Processed += ParseErrors;

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
                if (_document.IsProcessing)
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

                _dataSource.AddErrors(errors);
            }, VsTaskRunContext.UIThreadBackgroundPriority);
        }

        private IEnumerable<ErrorListItem> CreateErrorListItem(ParseItem item)
        {
            ITextSnapshotLine line = _docView.TextBuffer.CurrentSnapshot.GetLineFromPosition(item.Span.Start);

            foreach (Error error in item.Errors)
            {
                yield return new ErrorListItem
                {
                    ProjectName = _project?.Name ?? "",
                    FileName = _docView.FilePath,
                    Message = error.Message,
                    ErrorCategory = "syntax",
                    Severity = GetVsCategory(error.Severity),
                    Line = line.LineNumber,
                    Column = item.Span.Start - line.Start.Position,
                    BuildTool = Vsix.Name,
                    ErrorCode = error.ErrorCode
                };
            }
        }

        private __VSERRORCATEGORY GetVsCategory(ErrorSeverity category)
        {
            return category switch
            {
                ErrorSeverity.Message => __VSERRORCATEGORY.EC_MESSAGE,
                ErrorSeverity.Warning => __VSERRORCATEGORY.EC_WARNING,
                _ => __VSERRORCATEGORY.EC_ERROR,
            };
        }

        protected override void Closed(IWpfTextView textView)
        {
            _document.Processed -= ParseErrors;
            _dataSource.CleanAllErrors();
        }
    }
}

