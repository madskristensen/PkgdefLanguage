using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class Entry
    {
        public Entry(ParseItem registryKey)
        {
            RegistryKey = registryKey;
        }

        public ParseItem RegistryKey { get; }
        public List<Property> Properties { get; } = new();

        public int Start => RegistryKey.Start;
        public int End => Properties.Any() ? Properties.Last().Value.End : RegistryKey.End;
        public int Length => End - Start;

        public static implicit operator Span(Entry entry)
        {
            return Span.FromBounds(entry.Start, entry.End);
        }

        public bool Contains(int position)
        {
            return position >= Start && position <= End;
        }
    }
}
