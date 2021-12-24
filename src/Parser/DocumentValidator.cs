using System;
using System.Linq;

namespace PkgdefLanguage
{
    public partial class Document
    {
        public bool IsValid { get; set; }

        private class Errors
        {
            public static Error PL001 { get; } = new("PL001", "Unknown token at this location", ErrorSeverity.Error);
            public static Error PL002 { get; } = new("PL002", "Unclosed registry key entry.Add the missing ] character", ErrorSeverity.Error);
            public static Error PL003 { get; } = new("PL003", "Use the backslash character as delimiter instead of forward slash.", ErrorSeverity.Error);
            public static Error PL004 { get; } = new("PL004", "To set a registry key's default value, use '@' without quotation marks", ErrorSeverity.Warning);
            public static Error PL005 { get; } = new("PL005", "Value names must be enclosed in quotation marks.", ErrorSeverity.Error);
            public static Error PL006 { get; } = new("PL006", "The variable \"{0}\" doens't exist.", ErrorSeverity.Warning);
        }

        private void ValidateDocument()
        {
            IsValid = true;

            foreach (ParseItem item in Items)
            {
                // Unknown symbols
                if (item.Type == ItemType.Unknown)
                {
                    item.AddError(Errors.PL001);
                }

                // Registry key
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]"))
                    {
                        item.AddError(Errors.PL002);
                    }
                    else if (trimmedText.Contains("/") && !trimmedText.Contains("\\/"))
                    {
                        item.AddError(Errors.PL003);
                    }
                }

                // Properties
                else if (item.Type == ItemType.Operator)
                {
                    ParseItem name = item.Previous;
                    ParseItem value = item.Next;

                    if (name?.Type == ItemType.String)
                    {
                        if (name.Text == "\"@\"")
                        {
                            name.AddError(Errors.PL004);
                        }
                    }
                    else if (name?.Type == ItemType.Literal && name?.Text != "@")
                    {
                        name.AddError(Errors.PL005);
                    }
                }

                // Make sure strings are correctly closed with quotation mark
                if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\""))
                    {
                        item.AddError(Errors.PL005);
                    }
                }

                // Unknown references
                foreach (Reference reference in item.References)
                {
                    if (!PredefinedVariables.Variables.Any(v => v.Key.Equals(reference.Value.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        reference.Value.AddError(Errors.PL006.WithFormat(reference.Value.Text));
                    }
                }
            }
        }
    }
}
