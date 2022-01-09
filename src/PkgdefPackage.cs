global using Community.VisualStudio.Toolkit;
global using Task = System.Threading.Tasks.Task;
using System;
using System.ComponentModel.Design;
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

    [ProvideLanguageService(typeof(LanguageFactory), Constants.LanguageName, 0, DefaultToInsertSpaces = true, EnableLineNumbers = true, EnableAsyncCompletion = true, EnableCommenting = true, ShowCompletion = true, ShowDropDownOptions = true)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgDefExt)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgUndefExt)]
    [ProvideEditorExtension(typeof(LanguageFactory), Constants.PkgDefExt, 0x32)]
    [ProvideEditorExtension(typeof(LanguageFactory), Constants.PkgUndefExt, 0x32)]
    [ProvideEditorFactory(typeof(LanguageFactory), 0, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.Code_string, IsTrusted = true)]
    [ProvideFileIcon(Constants.PkgDefExt, "KnownMonikers.RegistrationScript")]
    [ProvideFileIcon(Constants.PkgUndefExt, "KnownMonikers.RegistrationScript")]
    public sealed class PkgdefPackage : ToolkitPackage
    {
        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var language = new LanguageFactory(this);
            RegisterEditorFactory(language);
            ((IServiceContainer)this).AddService(typeof(LanguageFactory), language, true);
            return Task.CompletedTask;
        }
    }
}
