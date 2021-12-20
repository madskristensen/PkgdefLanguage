using System;
using System.Linq;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private void ValidateDocument()
        {
            foreach (ParseItem item in Items)
            {
                // Unknown symbols
                if (item.Type == ItemType.Unknown)
                {
                    item.Errors.Add("Unknown token at this location.");
                }

                // Registry key
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]"))
                    {
                        item.Errors.Add("Unclosed registry key entry. Add the missing ] character");
                    }

                    if (trimmedText.Contains("/") && !trimmedText.Contains("\\/"))
                    {
                        item.Errors.Add("Use the backslash character as delimiter instead of forward slash.");
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
                            name.Errors.Add("To set a registry key's default value, use '@' without quotation marks");
                        }
                    }
                    else if (name?.Type == ItemType.Literal && name?.Text != "@")
                    {
                        name.Errors.Add("Value names must be enclosed in quotation marks.");
                    }
                }

                // Make sure strings are correctly closed with quotation mark
                if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\""))
                    {
                        item.Errors.Add("Value names must be enclosed in quotation marks.");
                    }
                }

                // Unknown references
                foreach (Reference reference in item.References)
                {
                    if (!CompletionCatalog.Variables.Any(v => v.Key.Equals(reference.Value.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        reference.Value.Errors.Add($"The variable \"{item.Text}\" doens't exist.");
                    }
                }
            }
        }
    }
}
