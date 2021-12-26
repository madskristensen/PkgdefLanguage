using System.Collections.Generic;
using System.ComponentModel.Composition;
using BaseClasses;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace PkgdefLanguage
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IClassificationTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    public class SyntaxHighligting : TokenClassificationBaseTagger
    {
        public override Dictionary<object, string> ClassificationMap { get; } = new()
        {
            { ItemType.RegistryKey, PredefinedClassificationTypeNames.SymbolDefinition },
            { ItemType.String, PredefinedClassificationTypeNames.String },
            { ItemType.Literal, PredefinedClassificationTypeNames.Literal },
            { ItemType.Comment, PredefinedClassificationTypeNames.Comment },
            { ItemType.ReferenceBraces, PredefinedClassificationTypeNames.SymbolDefinition },
            { ItemType.ReferenceName, PredefinedClassificationTypeNames.SymbolReference },
            { ItemType.Operator, PredefinedClassificationTypeNames.Operator },
        };
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IStructureTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    public class Outlining : TokenOutliningBaseTagger
    {
        // Adds outlining support based on the TokenTag.SupportsOutlining property.
    }

    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    public class ErrorSquigglies : TokenErrorBaseTagger
    {

    }

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name(Constants.LanguageName)]
    internal sealed class Tooltips : TokenQuickInfoBase
    {

    }
}
