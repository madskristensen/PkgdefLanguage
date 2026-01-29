using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PkgdefLanguage.Test
{
    /// <summary>
    /// Tests for predefined variables recognition.
    /// Verifies all known Visual Studio pkgdef variables are correctly identified.
    /// </summary>
    [TestClass]
    public class PredefinedVariablesTests
    {
        [TestMethod]
        public void AllPredefinedVariables_ShouldHaveDescriptions()
        {
            var variables = PredefinedVariables.Variables;

            Assert.IsNotEmpty(variables, "Should have predefined variables");

            foreach (var kvp in variables)
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(kvp.Key),
                    "Variable name should not be empty");
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(kvp.Value),
                    $"Variable '{kvp.Key}' should have a description");
            }
        }

        [TestMethod]
        public void PredefinedVariables_ContainsRootKey()
        {
            Assert.IsTrue(
                PredefinedVariables.Variables.ContainsKey("RootKey"),
                "Should contain RootKey variable");
        }

        [TestMethod]
        public void PredefinedVariables_ContainsBaseInstallDir()
        {
            Assert.IsTrue(
                PredefinedVariables.Variables.ContainsKey("BaseInstallDir"),
                "Should contain BaseInstallDir variable");
        }

        [TestMethod]
        public void PredefinedVariables_ContainsPackageFolder()
        {
            Assert.IsTrue(
                PredefinedVariables.Variables.ContainsKey("PackageFolder"),
                "Should contain PackageFolder variable");
        }

        [TestMethod]
        public void PredefinedVariables_ContainsCommonSystemVariables()
        {
            var expectedVariables = new[]
            {
                "AppDataLocalFolder",
                "ApplicationExtensionsFolder",
                "AppName",
                "BaseInstallDir",
                "CommonFiles",
                "Initialization",
                "MyDocuments",
                "PackageFolder",
                "ProgramFiles",
                "RootFolder",
                "RootKey",
                "ShellFolder",
                "System",
                "WinDir"
            };

            foreach (var variable in expectedVariables)
            {
                Assert.IsTrue(
                    PredefinedVariables.Variables.ContainsKey(variable),
                    $"Should contain '{variable}' variable");
            }
        }

        [TestMethod]
        public void PredefinedVariables_Count()
        {
            // This test documents the expected count of predefined variables
            // Update this if new variables are added
            Assert.HasCount(14, PredefinedVariables.Variables,
                "Expected number of predefined variables");
        }
    }
}
