using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RestClientVS;

namespace PkgdefLanguage
{
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.PkgdefLanguageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideLanguageService(typeof(Language), Language.LanguageName, 0, MatchBraces = true, MatchBracesAtCaret = true, EnableAsyncCompletion = true, EnableCommenting = true, ShowCompletion = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(Language), Language.FileExtension)]
    [ProvideFileIcon(Language.FileExtension, "KnownMonikers.RegistrationScript")]
    [ProvideBraceCompletion(Language.LanguageName)]

    [ProvideEditorFactory(typeof(Language), 0, false, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(Language), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    public sealed class PkgdefPackage : ToolkitPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterEditorFactory(new Language(this));
            return base.InitializeAsync(cancellationToken, progress);
        }
    }
}
