global using Community.VisualStudio.Toolkit;
global using Task = System.Threading.Tasks.Task;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace PkgdefLanguage
{
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.PkgdefLanguageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideLanguageService(typeof(LanguageService), Constants.LanguageName, 0, MatchBraces = true, MatchBracesAtCaret = true, EnableAsyncCompletion = true, EnableCommenting = true, ShowCompletion = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(LanguageService), Constants.PkgDefExt)]
    [ProvideLanguageExtension(typeof(LanguageService), Constants.PkgUndefExt)]
    [ProvideFileIcon(Constants.PkgDefExt, "KnownMonikers.RegistrationScript")]
    [ProvideFileIcon(Constants.PkgUndefExt, "KnownMonikers.RegistrationScript")]
    [ProvideEditorFactory(typeof(LanguageService), 0, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(LanguageService), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    public sealed class PkgdefPackage : ToolkitPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterEditorFactory(new LanguageService(this));
            return Task.CompletedTask;
        }
    }
}
