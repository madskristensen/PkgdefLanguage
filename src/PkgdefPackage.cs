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
    [ProvideOptionPage(typeof(OptionsProvider.AdvancedOptionsPage), "PkgdefLanguage", "Advanced", 0, 0, true, SupportsProfiles = true, NoShowAllView = true)]

    [ProvideLanguageService(typeof(LanguageFactory), Constants.LanguageName, 0, EnableLineNumbers = true, EnableAsyncCompletion = true, ShowCompletion = true, EnableFormatSelection = false, ShowDropDownOptions = true)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgDefExt)]
    [ProvideLanguageExtension(typeof(LanguageFactory), Constants.PkgUndefExt)]

    [ProvideEditorFactory(typeof(LanguageFactory), 738, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(LanguageFactory), Constants.PkgDefExt, 65535, NameResourceID = 738)]
    [ProvideEditorExtension(typeof(LanguageFactory), Constants.PkgUndefExt, 65535, NameResourceID = 738)]
    [ProvideEditorLogicalView(typeof(LanguageFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideFileIcon(Constants.PkgDefExt, "KnownMonikers.RegistrationScript")]
    [ProvideFileIcon(Constants.PkgUndefExt, "KnownMonikers.RegistrationScript")]
    public sealed class PkgdefPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var language = new LanguageFactory(this);
            RegisterEditorFactory(language);
            ((IServiceContainer)this).AddService(typeof(LanguageFactory), language, true);

            await Formatting.InitializeAsync();
            await Commenting2.InitializeAsync();
        }
    }
}
