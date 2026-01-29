using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;
using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    /// <summary>
    /// Tests for document validation error codes (PL001-PL011).
    /// These tests verify the validator correctly identifies errors without VS API dependencies.
    /// </summary>
    [TestClass]
    public class ValidationTests
    {
        #region PL001 - Unknown token

        [TestMethod]
        public async Task PL001_UnknownToken_AtStartOfDocument()
        {
            var lines = new[] { "unknown_token" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
            var unknownItem = doc.Items.FirstOrDefault(i => i.Type == ItemType.Unknown);
            Assert.IsNotNull(unknownItem);
            Assert.IsTrue(unknownItem.Errors.Any(e => e.ErrorCode == "PL001"));
        }

        [TestMethod]
        public async Task PL001_UnknownToken_RandomText()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\"\r\n",
                "random garbage text"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
            var unknownItem = doc.Items.FirstOrDefault(i => i.Type == ItemType.Unknown);
            Assert.IsNotNull(unknownItem);
            Assert.IsTrue(unknownItem.Errors.Any(e => e.ErrorCode == "PL001"));
        }

        #endregion

        #region PL002 - Unclosed registry key

        [TestMethod]
        public async Task PL002_UnclosedRegistryKey_MissingBracket()
        {
            var lines = new[] { "[test\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
            var registryKey = doc.Items.FirstOrDefault(i => i.Type == ItemType.RegistryKey);
            Assert.IsNotNull(registryKey);
            Assert.IsTrue(registryKey.Errors.Any(e => e.ErrorCode == "PL002"));
        }

        [TestMethod]
        public async Task PL002_ClosedRegistryKey_NoError()
        {
            var lines = new[] { "[test]\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var registryKey = doc.Items.FirstOrDefault(i => i.Type == ItemType.RegistryKey);
            Assert.IsNotNull(registryKey);
            Assert.IsFalse(registryKey.Errors.Any(e => e.ErrorCode == "PL002"));
        }

        #endregion

        #region PL003 - Forward slash in registry key

        [TestMethod]
        public async Task PL003_ForwardSlash_InRegistryKey()
        {
            var lines = new[] { "[HKEY_LOCAL_MACHINE/Software/Test]\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
            var registryKey = doc.Items.FirstOrDefault(i => i.Type == ItemType.RegistryKey);
            Assert.IsNotNull(registryKey);
            Assert.IsTrue(registryKey.Errors.Any(e => e.ErrorCode == "PL003"));
        }

        [TestMethod]
        public async Task PL003_BackSlash_NoError()
        {
            var lines = new[] { "[HKEY_LOCAL_MACHINE\\Software\\Test]\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var registryKey = doc.Items.FirstOrDefault(i => i.Type == ItemType.RegistryKey);
            Assert.IsNotNull(registryKey);
            Assert.IsFalse(registryKey.Errors.Any(e => e.ErrorCode == "PL003"));
        }

        #endregion

        #region PL004 - Quoted @ for default value

        [TestMethod]
        public async Task PL004_QuotedAtSign_Warning()
        {
            var lines = new[] { "[test]\r\n", "\"@\"=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);

            var nameItem = entries[0].Properties[0].Name;
            Assert.IsTrue(nameItem.Errors.Any(e => e.ErrorCode == "PL004"));
        }

        [TestMethod]
        public async Task PL004_UnquotedAtSign_NoError()
        {
            var lines = new[] { "[test]\r\n", "@=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Name.IsValid);
        }

        #endregion

        #region PL005 - Unquoted property name

        [TestMethod]
        public async Task PL005_UnquotedPropertyName_ParsedAsUnknown()
        {
            // When the property name is unquoted, the entire line doesn't match
            // the property regex, so it becomes an Unknown token with PL001
            var lines = new[] { "[test]\r\n", "PropertyName=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
            var unknownItem = doc.Items.FirstOrDefault(i => i.Type == ItemType.Unknown);
            Assert.IsNotNull(unknownItem);
            // Gets PL001 (unknown token) since the line doesn't parse as a valid property
            Assert.IsTrue(unknownItem.Errors.Any(e => e.ErrorCode == "PL001"));
        }

        [TestMethod]
        public async Task PL005_QuotedPropertyName_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"PropertyName\"=\"value\"" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Name.IsValid);
        }

        #endregion

        #region PL006 - Unknown variable

        [TestMethod]
        public async Task PL006_UnknownVariable_Warning()
        {
            var lines = new[] { "[$unknownvar$\\test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);

            var reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.IsNotNull(reference);
            Assert.IsTrue(reference.Errors.Any(e => e.ErrorCode == "PL006"));
        }

        [TestMethod]
        public async Task PL006_KnownVariable_NoError()
        {
            var lines = new[] { "[$RootKey$\\test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);

            var reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.IsNotNull(reference);
            Assert.IsFalse(reference.Errors.Any(e => e.ErrorCode == "PL006"));
        }

        #endregion

        #region PL007 - Variable missing closing $

        [TestMethod]
        public async Task PL007_VariableMissingClosingDollar_Error()
        {
            var lines = new[] { "[$RootKey\\test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);

            var reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.IsNotNull(reference);
            Assert.IsTrue(reference.Errors.Any(e => e.ErrorCode == "PL007"));
        }

        [TestMethod]
        public async Task PL007_VariableProperlyFormatted_NoError()
        {
            var lines = new[] { "[$RootKey$\\test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);

            var reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.IsNotNull(reference);
            Assert.IsFalse(reference.Errors.Any(e => e.ErrorCode == "PL007"));
        }

        #endregion

        #region PL008 - Duplicate registry key

        [TestMethod]
        public async Task PL008_DuplicateRegistryKey_Warning()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value1\"\r\n",
                "[test]\r\n",
                "@=\"value2\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var registryKeys = doc.Items.Where(i => i.Type == ItemType.RegistryKey).ToList();
            Assert.HasCount(2, registryKeys);

            // Second key should have duplicate warning
            Assert.IsTrue(registryKeys[1].Errors.Any(e => e.ErrorCode == "PL008"));
        }

        [TestMethod]
        public async Task PL008_DuplicateRegistryKey_CaseInsensitive()
        {
            var lines = new[] {
                "[TEST]\r\n",
                "@=\"value1\"\r\n",
                "[test]\r\n",
                "@=\"value2\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var registryKeys = doc.Items.Where(i => i.Type == ItemType.RegistryKey).ToList();
            Assert.HasCount(2, registryKeys);

            // Second key should have duplicate warning (case-insensitive)
            Assert.IsTrue(registryKeys[1].Errors.Any(e => e.ErrorCode == "PL008"));
        }

        [TestMethod]
        public async Task PL008_UniqueRegistryKeys_NoError()
        {
            var lines = new[] {
                "[test1]\r\n",
                "@=\"value1\"\r\n",
                "[test2]\r\n",
                "@=\"value2\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var registryKeys = doc.Items.Where(i => i.Type == ItemType.RegistryKey).ToList();
            Assert.HasCount(2, registryKeys);

            Assert.IsFalse(registryKeys[0].Errors.Any(e => e.ErrorCode == "PL008"));
            Assert.IsFalse(registryKeys[1].Errors.Any(e => e.ErrorCode == "PL008"));
        }

        #endregion

        #region PL009 - Invalid dword value

        [TestMethod]
        public async Task PL009_DWordTooShort_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=dword:123" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL009"));
        }

        [TestMethod]
        public async Task PL009_DWordTooLong_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=dword:123456789" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL009"));
        }

        [TestMethod]
        public async Task PL009_DWordInvalidChars_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=dword:0000ZZZZ" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL009"));
        }

        [TestMethod]
        public async Task PL009_DWordValid_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=dword:0000007b" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.IsValid);
        }

        [TestMethod]
        public async Task PL009_DWordValidUppercase_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=dword:DEADBEEF" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.IsValid);
        }

        #endregion

        #region PL010 - Invalid qword value

        [TestMethod]
        public async Task PL010_QWordTooShort_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=qword:00000000" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL010"));
        }

        [TestMethod]
        public async Task PL010_QWordTooLong_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=qword:00000000000000001" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL010"));
        }

        [TestMethod]
        public async Task PL010_QWordValid_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"Value\"=qword:00000000ffffffff" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.IsValid);
        }

        #endregion

        #region PL011 - Invalid hex value

        [TestMethod]
        public async Task PL011_HexInvalidByteLength_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Binary\"=hex:0,02,03" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL011"));
        }

        [TestMethod]
        public async Task PL011_HexInvalidChars_Error()
        {
            var lines = new[] { "[test]\r\n", "\"Binary\"=hex:ZZ,02,03" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL011"));
        }

        [TestMethod]
        public async Task PL011_HexValid_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"Binary\"=hex:01,02,03,ff" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.IsValid);
        }

        [TestMethod]
        public async Task PL011_HexWithType_Valid_NoError()
        {
            var lines = new[] { "[test]\r\n", "\"String\"=hex(2):48,00,65,00" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entries = doc.Items.OfType<Entry>().ToList();
            Assert.HasCount(1, entries);
            Assert.IsTrue(entries[0].Properties[0].Value.IsValid);
        }

        #endregion

        #region Document.IsValid property

        [TestMethod]
        public async Task DocumentIsValid_WhenNoErrors()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsTrue(doc.IsValid);
        }

        [TestMethod]
        public async Task DocumentIsValid_FalseWhenErrors()
        {
            var lines = new[] { "[test" }; // Missing closing bracket

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsFalse(doc.IsValid);
        }

        [TestMethod]
        public async Task DocumentIsValid_EmptyDocument()
        {
            var lines = new[] { "" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsTrue(doc.IsValid);
        }

        [TestMethod]
        public async Task DocumentIsValid_OnlyComments()
        {
            var lines = new[] {
                "; This is a comment\r\n",
                "// Another comment"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.IsTrue(doc.IsValid);
        }

        #endregion
    }
}
