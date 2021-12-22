using System.Collections.Generic;
using System.Linq;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private string[] _lines;

        protected Document(string[] lines)
        {
            _lines = lines;
            _ = ParseAsync();
        }

        public bool IsValid { get; set; }

        public List<ParseItem> Items { get; private set; } = new List<ParseItem>();

        public List<Entry> Entries { get; private set; } = new();

        public void UpdateLines(string[] lines)
        {
            _lines = lines;
        }

        public static Document FromLines(params string[] lines)
        {
            var doc = new Document(lines);
            return doc;
        }
        public ParseItem GetTokenFromPosition(int position)
        {
            ParseItem item = Items.LastOrDefault(t => t.Span.Contains(position));
            ParseItem reference = item?.References.FirstOrDefault(v => v.Value != null && v.Value.Span.Contains(position))?.Value;

            // Return the reference if it exist; otherwise the item
            return reference ?? item;
        }
    }
}
