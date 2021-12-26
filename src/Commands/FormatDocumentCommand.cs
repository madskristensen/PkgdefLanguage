using System.ComponentModel.Composition;
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
                    sb.AppendLine();
                    sb.AppendLine(entry.GetFormattedText());
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

        public CommandState GetCommandState(FormatDocumentCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}