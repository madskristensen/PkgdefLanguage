using System.Collections.Generic;

namespace PkgdefLanguage
{
    internal sealed class PredefinedVariables
    {
        public static Dictionary<string, string> Variables = new()
        {
            { "AppDataLocalFolder", "The sub-folder under %LOCALAPPDATA% for this application." },
            { "ApplicationExtensionsFolder", "n/a" },
            { "AppName", "The qualified name of the application that is passed to the AppEnv.dll entry points. The qualified name consists of the application name, an underscore, and the class identifier (CLSID of the application automation object, which is also recorded as the value of the ThisVersionDTECLSID setting in the project .pkgdef file." },
            { "BaseInstallDir", "The full path of the location where Visual Studio was installed." },
            { "CommonFiles", "The value of the %CommonProgramFiles% environment variable." },
            { "Initialization", "n/a" },
            { "MyDocuments", "The full path of the My Documents folder of the current user." },
            { "PackageFolder", "The full path of the directory that contains the package assembly files for the application." },
            { "ProgramFiles", "The value of the %ProgramFiles% environment variable." },
            { "RootFolder", "The full path of the root directory of the application." },
            { "RootKey", "The root registry key for the application. By default the root is in HKEY_CURRENT_USER\\Software\\CompanyName\\ProjectName\\VersionNumber (when the application is running, _Config is appended to this key. It is set by the RegistryRoot value in the SolutionName.pkgdef file." },
            { "ShellFolder", "The full path of the location where Visual Studio was installed." },
            { "System", "The Windows\\system32 folder." },
            { "WinDir", "The Windows folder." },
        };
    }
}
