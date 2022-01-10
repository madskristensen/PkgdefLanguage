using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace PkgdefLanguage
{
    public class Commenting2
    {
        public static async Task InitializeAsync()
        {
            // We need to manually intercept the commenting command, because language services swallow these commands.
            await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.COMMENT_BLOCK, () => Execute(Comment));
            await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK, () => Execute(Uncomment));
        }

        private static CommandProgression Execute(Action<DocumentView> action)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();

                if (doc?.TextBuffer != null && doc.TextBuffer.ContentType.IsOfType(Constants.LanguageName))
                {
                    action(doc);
                    return CommandProgression.Stop;
                }

                return CommandProgression.Continue;
            });
        }

        private static void Comment(DocumentView doc)
        {
            SnapshotSpan spans = doc.TextView.Selection.SelectedSpans.First();
            Collection<ITextViewLine> lines = doc.TextView.TextViewLines.GetTextViewLinesIntersectingSpan(spans);

            foreach (ITextViewLine line in lines.Reverse())
            {
                doc.TextBuffer.Insert(line.Start.Position, Constants.CommentChars[0]);
            }
        }

        private static void Uncomment(DocumentView doc)
        {
            SnapshotSpan spans = doc.TextView.Selection.SelectedSpans.First();
            Collection<ITextViewLine> lines = doc.TextView.TextViewLines.GetTextViewLinesIntersectingSpan(spans);

            foreach (ITextViewLine line in lines.Reverse())
            {
                var span = Span.FromBounds(line.Start, line.End);
                var originalText = doc.TextBuffer.CurrentSnapshot.GetText(span).TrimStart('/', ';');
                Span commentCharSpan = new(span.Start, span.Length - originalText.Length);

                doc.TextBuffer.Delete(commentCharSpan);
            }
        }
    }
}