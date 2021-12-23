using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class Entry : ParseItem
    {
        public Entry(ParseItem registryKey, Document document)
            : base(registryKey.Span.Start, registryKey.Text, document, ItemType.Entry)
        {
            RegistryKey = registryKey;
        }

        public ParseItem RegistryKey { get; }
        public List<Property> Properties { get; } = new();

        public override Span Span
        {
            get
            {
                var end = Properties.Any() ? Properties.Last().Value.Span.End : RegistryKey.Span.End;
                return Span.FromBounds(RegistryKey.Span.Start, end);
            }
        }

        public string GetFormattedText()
        {
            var sb = new StringBuilder();

            if (RegistryKey != null)
            {
                sb.AppendLine(RegistryKey.Text.Trim());
                foreach (Property property in Properties)
                {
                    sb.AppendLine($"{property.Name.Text.Trim()}={property.Value.Text.Trim()}");
                }
            }

            return sb.ToString().Trim();
        }
    }
}
