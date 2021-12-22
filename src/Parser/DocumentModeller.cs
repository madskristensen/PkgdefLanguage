using System.Collections.Generic;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private void CreateSemanticModel()
        {
            List<Entry> entries = new();
            Entry currentEntry = null;
            ParseItem propName = null;

            foreach (ParseItem item in Items)
            {
                if (item.Type == ItemType.RegistryKey)
                {
                    currentEntry = new Entry(item);
                    entries.Add(currentEntry);
                }
                else if (item.Type == ItemType.String || item.Type == ItemType.Literal)
                {
                    if (propName == null)
                    {
                        propName = item;

                    }
                    else
                    {
                        var property = new Property(propName, item);
                        currentEntry?.Properties.Add(property);
                        propName = null;
                    }
                }
            }

            Entries = entries;
        }
    }
}
