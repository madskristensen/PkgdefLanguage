using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;
using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    /// <summary>
    /// Tests for Entry and Property classes.
    /// Verifies correct behavior of registry entry parsing structures.
    /// </summary>
    [TestClass]
    public class EntryTests
    {
        #region Entry.Span tests

        [TestMethod]
        public async Task Entry_Span_WithNoProperties()
        {
            var lines = new[] { "[test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.AreEqual(0, entry.Span.Start);
            Assert.AreEqual(6, entry.Span.End);
        }

        [TestMethod]
        public async Task Entry_Span_WithProperties()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.AreEqual(0, entry.Span.Start);
            // Span should extend to end of last property value
            Assert.IsGreaterThan(entry.RegistryKey.Span.End, entry.Span.End);
        }

        [TestMethod]
        public async Task Entry_Span_WithMultipleProperties()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value1\"\r\n",
                "\"Prop\"=\"value2\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.HasCount(2, entry.Properties);

            // Span should extend to end of last property
            var lastProp = entry.Properties.Last();
            Assert.AreEqual(lastProp.Value.Span.End, entry.Span.End);
        }

        #endregion

        #region Entry.RegistryKey tests

        [TestMethod]
        public async Task Entry_RegistryKey_SimpleKey()
        {
            var lines = new[] { "[HKEY_LOCAL_MACHINE\\Software]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.AreEqual("[HKEY_LOCAL_MACHINE\\Software]", entry.RegistryKey.Text);
            Assert.AreEqual(ItemType.RegistryKey, entry.RegistryKey.Type);
        }

        [TestMethod]
        public async Task Entry_RegistryKey_WithVariable()
        {
            var lines = new[] { "[$RootKey$\\Extensions]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.HasCount(1, entry.RegistryKey.References);
            Assert.AreEqual("$RootKey$", entry.RegistryKey.References[0].Text);
        }

        #endregion

        #region Entry.Properties tests

        [TestMethod]
        public async Task Entry_Properties_Empty()
        {
            var lines = new[] { "[test]" };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.HasCount(0, entry.Properties);
        }

        [TestMethod]
        public async Task Entry_Properties_DefaultValue()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"default\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.HasCount(1, entry.Properties);
            Assert.AreEqual("@", entry.Properties[0].Name.Text);
        }

        [TestMethod]
        public async Task Entry_Properties_NamedProperty()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"MyProperty\"=\"MyValue\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);
            Assert.HasCount(1, entry.Properties);
            Assert.AreEqual("\"MyProperty\"", entry.Properties[0].Name.Text);
            Assert.AreEqual("\"MyValue\"", entry.Properties[0].Value.Text);
        }

        #endregion

        #region Entry.GetFormattedText tests

        [TestMethod]
        public async Task Entry_GetFormattedText_Simple()
        {
            var lines = new[] {
                "[test]\r\n",
                "@=\"value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var formatted = entry.GetFormattedText();
            Assert.Contains("[test]", formatted);
            Assert.Contains("@=\"value\"", formatted);
        }

        [TestMethod]
        public async Task Entry_GetFormattedText_TrimsWhitespace()
        {
            var lines = new[] {
                "  [test]  \r\n",
                "  @=\"value\"  "
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var formatted = entry.GetFormattedText();
            Assert.StartsWith("[test]", formatted);
            Assert.DoesNotStartWith("  ", formatted);
        }

        #endregion

        #region Property tests

        [TestMethod]
        public async Task Property_ToString()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"Name\"=\"Value\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var property = entry.Properties[0];
            var str = property.ToString();
            Assert.AreEqual("\"Name\"=\"Value\"", str);
        }

        [TestMethod]
        public async Task Property_DWordValue()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"Count\"=dword:00000001"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var property = entry.Properties[0];
            Assert.AreEqual("\"Count\"", property.Name.Text);
            Assert.AreEqual("dword:00000001", property.Value.Text);
            Assert.AreEqual(ItemType.Literal, property.Value.Type);
        }

        [TestMethod]
        public async Task Property_QWordValue()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"BigNumber\"=qword:0000000100000000"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var property = entry.Properties[0];
            Assert.AreEqual("qword:0000000100000000", property.Value.Text);
        }

        [TestMethod]
        public async Task Property_HexValue()
        {
            var lines = new[] {
                "[test]\r\n",
                "\"Binary\"=hex:01,02,03,04"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var entry = doc.Items.OfType<Entry>().FirstOrDefault();
            Assert.IsNotNull(entry);

            var property = entry.Properties[0];
            Assert.AreEqual("hex:01,02,03,04", property.Value.Text);
        }

        #endregion
    }
}
