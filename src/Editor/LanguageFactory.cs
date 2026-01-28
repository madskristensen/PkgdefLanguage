using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace PkgdefLanguage
{
    [ComVisible(true)]
    [Guid(PackageGuids.EditorFactoryString)]
    internal sealed class LanguageFactory : LanguageBase
    {
        private DropdownBars _dropdownBars;

        public LanguageFactory(object site) : base(site)
        { }

        public override string Name => Constants.LanguageName;

        public override string[] FileExtensions { get; } = new[] { Constants.PkgDefExt, Constants.PkgUndefExt };

        /// <summary>
        /// Creates a custom CodeWindowManager that implements IVsDocOutlineProvider
        /// to provide document outline support for the Document Outline tool window.
        /// </summary>
        public override CodeWindowManager CreateCodeWindowManager(IVsCodeWindow codeWindow, Source source)
        {
            // Get the IWpfTextView and Document from the code window
            if (codeWindow.GetPrimaryView(out IVsTextView vsTextView) == VSConstants.S_OK)
            {
                IWpfTextView textView = vsTextView.ToIWpfTextView();
                if (textView != null)
                {
                    Document document = textView.TextBuffer.GetDocument();
                    if (document != null)
                    {
                        return new PkgdefCodeWindowManager(this, codeWindow, source, textView, document);
                    }
                }
            }

            // Fall back to the default CodeWindowManager if we can't get the required objects
            return base.CreateCodeWindowManager(codeWindow, source);
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView textView)
        {
            _dropdownBars?.Dispose();
            _dropdownBars = new DropdownBars(textView, this);

            return _dropdownBars;
        }

        public override void SetDefaultPreferences(LanguagePreferences preferences)
        {
            preferences.EnableCodeSense = false;
            preferences.EnableMatchBraces = true;
            preferences.EnableMatchBracesAtCaret = true;
            preferences.EnableShowMatchingBrace = true;
            preferences.EnableCommenting = true;
            preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
            preferences.LineNumbers = true;
            preferences.MaxErrorMessages = 100;
            preferences.AutoOutlining = false;
            preferences.MaxRegionTime = 2000;
            preferences.InsertTabs = false;
            preferences.IndentSize = 2;
            preferences.IndentStyle = IndentingStyle.Smart;
            preferences.ShowNavigationBar = true;
            preferences.EnableFormatSelection = true;

            preferences.WordWrap = true;
            preferences.WordWrapGlyphs = true;

            preferences.AutoListMembers = true;
            preferences.HideAdvancedMembers = false;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = true;
        }

        public override void Dispose()
        {
            _dropdownBars?.Dispose();
            _dropdownBars = null;
            base.Dispose();
        }
    }
}
