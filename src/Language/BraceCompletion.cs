using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(IBraceCompletionContextProvider))]
    [BracePair('(', ')')]
    [BracePair('[', ']')]
    [BracePair('{', '}')]
    [BracePair('"', '"')]
    [BracePair('$', '$')]
    [ContentType(Language.LanguageName)]
    [Name(Language.LanguageName)]
    internal sealed class BraceCompletion : BraceCompletionBase
    {

    }
}
