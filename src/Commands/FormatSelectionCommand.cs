
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(FormatSelectionCommand))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class FormatSelectionCommand : ICommandHandler<FormatSelectionCommandArgs>
    {
        public string DisplayName => nameof(CommentCommand);

        public bool ExecuteCommand(FormatSelectionCommandArgs args, CommandExecutionContext executionContext)
        {
            PkgdefDocument doc = args.SubjectBuffer.GetDocument();
            SnapshotPoint position = args.TextView.Selection.Start.Position;
            ParseItem item = doc.Items.FirstOrDefault(i => i.Type == ItemType.Entry && i.Span.Contains(position));

            if (item is Entry entry)
            {
                args.SubjectBuffer.Replace(entry, entry.GetFormattedText());
            }

            return true;
        }

        public CommandState GetCommandState(FormatSelectionCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}