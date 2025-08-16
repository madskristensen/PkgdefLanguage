using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class Formatting
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
            
            // Pre-calculate capacity to reduce StringBuilder reallocations
            int estimatedCapacity = buffer.CurrentSnapshot.Length + (doc.Items.OfType<Entry>().Count() * 2);
            var sb = new StringBuilder(estimatedCapacity);

            // Cache entries for better performance
            var entries = doc.Items.OfType<Entry>().ToList();
            
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                var insertLineBefore = true;

                if (!entry.Properties.Any() && i + 1 < entries.Count)
                {
                    Entry next = entries[i + 1];
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

            // Add comments separately for better performance
            foreach (ParseItem item in doc.Items.Where(i => i.Type == ItemType.Comment))
            {
                sb.AppendLine(item.Text.Trim());
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