using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PkgdefLanguage.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public class ParseTest
    {
        [TestMethod]
        public async Task SingleEntry()
        {
            var lines = new[] { "[test]\r\n",
                                 "@=\"test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.AreEqual(1, entries.Count);

            Entry entry = entries.First();
            Assert.AreEqual(1, entry.Properties.Count);
            Assert.AreEqual("@", entry.Properties.First().Name.Text);
            Assert.AreEqual(16, entry.Span.End);
        }

        [TestMethod]
        public async Task EqualsInPropertyValue()
        {
            var lines = new[] { "[test]\r\n",
                                 "@=\"test=1234\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.AreEqual(1, entries.Count);

            Entry entry = entries.First();
            Assert.AreEqual("\"test=1234\"", entry.Properties.First().Value.Text);
        }

        [TestMethod]
        public async Task MultipleEntries()
        {
            var lines = new[] { "[test]\r\n",
                         "@=\"test\"",
                         "\"test\"=\"test\"",
                         "[test]\r\n",
                         "@=\"test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();


            Assert.AreEqual(2, entries.Count);

            Entry first = entries.First();
            Entry second = entries.Last();

            Assert.AreEqual(2, first.Properties.Count);
            Assert.AreEqual(29, first.Span.End);
            Assert.AreEqual("@", second.Properties.First().Name.Text);
            Assert.AreEqual(45, second.Span.End);
        }

        [TestMethod]
        public async Task Comment()
        {
            var lines = new[] { "; comment\r\n",
                                "\r\n",
                                ";comment"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.AreEqual(2, doc.Items.Count);
            Assert.AreEqual(2, doc.Items.Where(i => i.Type == ItemType.Comment).Count());
        }

        [TestMethod]
        public async Task InvalidPropertyName()
        {
            var lines = new[] { "[test]\r\n",
                         "\"@\"=\"test\"",
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            ParseItem prop = entries[0].Properties[0].Name;

            Assert.IsFalse(prop.IsValid);
        }

        [TestMethod]
        public async Task VariableSpanCorrect()
        {
            var lines = new[] { "[$rootkey$]" };
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            ParseItem reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.AreEqual("$rootkey$", reference.Text);
            Assert.AreEqual(1, reference.Span.Start);
            Assert.AreEqual(10, reference.Span.End);
        }
    }
}
