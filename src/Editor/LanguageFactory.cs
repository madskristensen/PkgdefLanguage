using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace PkgdefLanguage
{
    [Guid(PackageGuids.EditorFactoryString)]
    internal sealed class LanguageFactory : LanguageBase
    {
        private DropdownBars _dropdownBars;
        private bool _disposed;

        public LanguageFactory(object site) : base(site)
        { }

        public override string Name => Constants.LanguageName;

        public override string[] FileExtensions { get; } = new[] { Constants.PkgDefExt, Constants.PkgUndefExt };

        public override void SetDefaultPreferences(LanguagePreferences preferences)
        {
            preferences.EnableCodeSense = false;
            preferences.EnableMatchBraces = true;
            preferences.EnableMatchBracesAtCaret = true;
            preferences.EnableShowMatchingBrace = true;
            preferences.EnableCommenting = false;
            preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
            preferences.LineNumbers = true;
            preferences.MaxErrorMessages = 100;
            preferences.AutoOutlining = false;
            preferences.MaxRegionTime = 2000;
            preferences.InsertTabs = false;
            preferences.IndentSize = 2;
            preferences.IndentStyle = IndentingStyle.Smart;
            preferences.ShowNavigationBar = true;

            preferences.WordWrap = true;
            preferences.WordWrapGlyphs = true;

            preferences.AutoListMembers = true;
            preferences.HideAdvancedMembers = false;
            preferences.EnableQuickInfo = true;
            preferences.ParameterInformation = true;
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView textView)
        {
            _dropdownBars?.Dispose();
            _dropdownBars = new DropdownBars(textView, this);

            return _dropdownBars;
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dropdownBars?.Dispose();
            _dropdownBars = null;

            base.Dispose();
        }
    }
}
