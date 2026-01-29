using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;
using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    /// <summary>
    /// Tests for parsing edge cases and unusual input.
    /// </summary>
    [TestClass]
    public class ParserEdgeCaseTests
    {
        #region Empty and whitespace

        [TestMethod]
        public async Task Parse_EmptyDocument()
        {
            var lines = new[] { "" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(0, doc.Items);
            Assert.IsTrue(doc.IsValid);
        }

        [TestMethod]
        public async Task Parse_WhitespaceOnlyLines()
        {
            var lines = new[] { "   \r\n", "\t\t\r\n", "  " };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            // Whitespace-only lines should not create entries
            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(0, entries);
        }

        [TestMethod]
        public async Task Parse_LeadingWhitespaceOnRegistryKey()
        {
            var lines = new[] { "   [test]\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
        }

        #endregion

        #region Line endings

        [TestMethod]
        public async Task Parse_UnixLineEndings()
        {
            var lines = new[] { "[test]\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
        }

        [TestMethod]
        public async Task Parse_NoLineEndings()
        {
            var lines = new[] { "[test]", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
        }

        #endregion

        #region Comments

        [TestMethod]
        public async Task Parse_SemicolonComment()
        {
            var lines = new[] { "; This is a semicolon comment" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(1, doc.Items);
            Assert.AreEqual(ItemType.Comment, doc.Items[0].Type);
        }

        [TestMethod]
        public async Task Parse_DoubleSlashComment()
        {
            var lines = new[] { "// This is a double-slash comment" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(1, doc.Items);
            Assert.AreEqual(ItemType.Comment, doc.Items[0].Type);
        }

        [TestMethod]
        public async Task Parse_CommentsInterspersedWithEntries()
        {
            var lines = new[] {
                "; Header comment\r\n",
                "[test]\r\n",
                "; Property comment\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var comments = doc.Items.Where(i => i.Type == ItemType.Comment).ToList();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(2, comments);
            Assert.HasCount(1, entries);
        }

        #endregion

        #region Preprocessor directives

        [TestMethod]
        public async Task Parse_IncludeDirective()
        {
            var lines = new[] { "#include \"common.pkgdef\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(1, doc.Items);
            Assert.AreEqual(ItemType.Preprocessor, doc.Items[0].Type);
        }

        [TestMethod]
        public async Task Parse_IncludeDirectiveWithPath()
        {
            var lines = new[] { "#include \"..\\shared\\common.pkgdef\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(1, doc.Items);
            Assert.AreEqual(ItemType.Preprocessor, doc.Items[0].Type);
        }

        #endregion

        #region Special characters in values

        [TestMethod]
        public async Task Parse_EscapedQuotesInValue()
        {
            // Note: pkgdef uses doubled backslashes for escaping
            var lines = new[] {
                "[test]\r\n",
                "\"Path\"=\"C:\\\\Users\\\\Test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.Contains("\\\\", entries[0].Properties[0].Value.Text);
        }

        [TestMethod]
        public async Task Parse_GuidInValue()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"CLSID\"=\"{12345678-ABCD-EF01-2345-6789ABCDEF01}\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.Contains("{12345678-ABCD-EF01-2345-6789ABCDEF01}", entries[0].Properties[0].Value.Text);
        }

        [TestMethod]
        public async Task Parse_SpecialCharsInRegistryKeyPath()
        {
            var lines = new[] { "[test\\Sub-Key_123]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.AreEqual("[test\\Sub-Key_123]", entries[0].RegistryKey.Text);
        }

        #endregion

        #region Multiple variables

        [TestMethod]
        public async Task Parse_MultipleVariablesInRegistryKey()
        {
            var lines = new[] { "[$RootKey$\\$BaseInstallDir$\\Extensions]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.HasCount(2, entries[0].RegistryKey.References);
        }

        [TestMethod]
        public async Task Parse_AdjacentVariables()
        {
            var lines = new[] { "[$RootKey$$PackageFolder$]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.HasCount(2, entries[0].RegistryKey.References);
        }

        #endregion

        #region Multiple entries

        [TestMethod]
        public async Task Parse_ManyEntries()
        {
            var lines = new[] {
                "[entry1]\r\n",
                "@=\"val1\"\r\n",
                "[entry2]\r\n",
                "@=\"val2\"\r\n",
                "[entry3]\r\n",
                "@=\"val3\"\r\n",
                "[entry4]\r\n",
                "@=\"val4\"\r\n",
                "[entry5]\r\n",
                "@=\"val5\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(5, entries);
        }

        [TestMethod]
        public async Task Parse_EntriesWithoutPropertiesFollowedByEntryWithProperties()
        {
            var lines = new[] {
                "[entry1]\r\n",
                "[entry2]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(2, entries);
            Assert.HasCount(0, entries[0].Properties);
            Assert.HasCount(1, entries[1].Properties);
        }

        #endregion

        #region ParseItem navigation

        [TestMethod]
        public async Task ParseItem_Previous_FirstItem()
        {
            var lines = new[] { "[test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            // The first item in the Items list should have null Previous
            Assert.IsNotEmpty(doc.Items);
            var firstItem = doc.Items[0];
            Assert.IsNull(firstItem.Previous);
        }

        [TestMethod]
        public async Task ParseItem_Previous_SecondItem()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            // Find an item that has a Previous
            Assert.IsGreaterThan(1, doc.Items.Count);
            var secondItem = doc.Items[1];
            Assert.IsNotNull(secondItem.Previous);
            Assert.AreEqual(doc.Items[0], secondItem.Previous);
        }

        [TestMethod]
        public async Task ParseItem_Next_Navigation()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            // Items should be linked via Next/Previous
            Assert.IsGreaterThan(1, doc.Items.Count);
            var firstItem = doc.Items[0];
            Assert.IsNotNull(firstItem.Next);
            Assert.AreEqual(doc.Items[1], firstItem.Next);
        }

        [TestMethod]
        public async Task ParseItem_Next_LastItem()
        {
            var lines = new[] { "[test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            // The last item should have null Next
            Assert.IsNotEmpty(doc.Items);
            var lastItem = doc.Items[doc.Items.Count - 1];
            Assert.IsNull(lastItem.Next);
        }

        #endregion

        #region Real-world patterns

        [TestMethod]
        public async Task Parse_TypicalPackageRegistration()
        {
            var lines = new[] {
                "[$RootKey$\\Packages\\{12345678-1234-1234-1234-123456789012}]\r\n",
                "@=\"MyPackage\"\r\n",
                "\"InprocServer32\"=\"$PackageFolder$\\MyPackage.dll\"\r\n",
                "\"Class\"=\"MyCompany.MyPackage.Package\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.HasCount(3, entries[0].Properties);
            Assert.IsTrue(doc.IsValid);
        }

        [TestMethod]
        public async Task Parse_TypicalMenuCommand()
        {
            var lines = new[] {
                "; Menu command registration\r\n",
                "[$RootKey$\\Menus]\r\n",
                "\"{12345678-1234-1234-1234-123456789012}\"=\", 1000, 1\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            var comments = doc.Items.Where(i => i.Type == ItemType.Comment).ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, comments);
            Assert.HasCount(1, entries[0].Properties);
        }

        #endregion
    }
}
