using System.ComponentModel.Composition;
using BaseClasses;
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
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class BraceCompletion : BraceCompletionBase
    {

    }
}
