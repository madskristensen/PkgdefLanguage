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

    [ProvideLanguageService(typeof(LanguageFactory), Constants.LanguageName, 0, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, EnableAsyncCompletion = true, EnableCommenting = true, ShowCompletion = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgDefExt)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgUndefExt)]
    [ProvideFileIcon(Constants.PkgDefExt, "KnownMonikers.RegistrationScript")]
    [ProvideFileIcon(Constants.PkgUndefExt, "KnownMonikers.RegistrationScript")]
    [ProvideEditorFactory(typeof(LanguageFactory), 0, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    public sealed class PkgdefPackage : ToolkitPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            RegisterEditorFactory(new LanguageFactory(this));
            return Task.CompletedTask;
        }
    }
}
