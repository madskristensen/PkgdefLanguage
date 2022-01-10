using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class FormatDocumentCommand
    {
        public static async Task InitializeAsync()
        {
            // We need to manually intercept the FormatDocument command, because language services swallow formatting commands.
            await VS.Commands.InterceptAsync(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.FORMATDOCUMENT, () =>
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();

                    if (doc?.TextBuffer != null && doc.TextBuffer.ContentType.IsOfType(Constants.LanguageName))
                    {
                        Format(doc.TextBuffer);
                        return CommandProgression.Stop;
                    }

                    return CommandProgression.Continue;
                });
            });
        }

        private static void Format(ITextBuffer buffer)
        {
            Document doc = buffer.GetDocument();
            var sb = new StringBuilder();

            foreach (ParseItem item in doc.Items)
            {
                if (item is Entry entry)
                {
                    var insertLineBefore = true;

                    if (!entry.Properties.Any() && NextEntry(entry) is Entry next)
                    {
                        var currentKey = entry.RegistryKey.Text.Trim().TrimEnd(']');
                        var nextKey = next.RegistryKey.Text.Trim().TrimEnd(']');

                        if (nextKey.IndexOf(currentKey, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            insertLineBefore = false;
                        }
                    }

                    sb.AppendLine();

                    if (insertLineBefore)
                    {
                        sb.AppendLine(entry.GetFormattedText());
                    }
                    else
                    {
                        sb.Append(entry.GetFormattedText());
                    }

                }
                else if (item.Type == ItemType.Comment)
                {
                    sb.AppendLine(item.Text.Trim());
                }
            }

            var wholeDocSpan = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(wholeDocSpan, sb.ToString().Trim());
        }

        private static Entry NextEntry(Entry current)
        {
            return current.Document.Items
                .OfType<Entry>()
                .FirstOrDefault(e => e.Span.Start >= current.Span.End);
        }
    }
}