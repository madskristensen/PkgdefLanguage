using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PkgdefLanguage.Test
{
#pragma warning disable IDE1006 // Naming Styles

    [TestClass]
    public class ParseTest
    {
        [TestMethod]
        public async Task SimpleEntry()
        {
            var lines = new[] { "[test]\r\n",
                                 "@=\"test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.AreEqual(1, doc.Entries.Count);

            Entry entry = doc.Entries.First();
            Assert.AreEqual(1, entry.Properties.Count);
            Assert.AreEqual("@", entry.Properties.First().Name.Text);
            Assert.AreEqual(16, entry.End);
        }

        [TestMethod]
        public async Task Comment()
        {
            var lines = new[] { "; comment\r\n",
                                "\r\n",
                                "; comment"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.AreEqual(2, doc.Items.Count);
            Assert.AreEqual(2, doc.Items.Where(i => i.Type == ItemType.Comment).Count());
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
