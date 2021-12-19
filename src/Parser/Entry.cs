using System.Collections.Generic;
using System.Linq;

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

        public bool Contains(int position)
        {
            return position >= Start && position <= End;
        }
    }
}
