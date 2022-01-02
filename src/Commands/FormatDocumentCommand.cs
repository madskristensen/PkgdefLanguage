using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(FormatDocumentCommand))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class FormatDocumentCommand : ICommandHandler<FormatDocumentCommandArgs>
    {
        public string DisplayName => nameof(CommentCommand);

        public bool ExecuteCommand(FormatDocumentCommandArgs args, CommandExecutionContext executionContext)
        {
            Document doc = args.SubjectBuffer.GetDocument();
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

            var wholeDocSpan = new Span(0, args.SubjectBuffer.CurrentSnapshot.Length);
            args.SubjectBuffer.Replace(wholeDocSpan, sb.ToString().Trim());

            return true;
        }

        private Entry NextEntry(Entry current)
        {
            return current.Document.Items
                .OfType<Entry>()
                .FirstOrDefault(e => e.Span.Start >= current.Span.End);
        }

        public CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}