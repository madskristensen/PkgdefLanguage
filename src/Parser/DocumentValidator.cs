using System;
using System.Linq;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private void ValidateDocument()
        {
            IsValid = true;

            foreach (ParseItem item in Items)
            {
                // Unknown symbols
                if (item.Type == ItemType.Unknown)
                {
                    item.AddError("Unknown token at this location.");
                }

                // Registry key
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]"))
                    {
                        item.AddError("Unclosed registry key entry. Add the missing ] character");
                    }

                    if (trimmedText.Contains("/") && !trimmedText.Contains("\\/"))
                    {
                        item.AddError("Use the backslash character as delimiter instead of forward slash.");
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
                            name.AddError("To set a registry key's default value, use '@' without quotation marks");
                        }
                    }
                    else if (name?.Type == ItemType.Literal && name?.Text != "@")
                    {
                        name.AddError("Value names must be enclosed in quotation marks.");
                    }
                }

                // Make sure strings are correctly closed with quotation mark
                if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\""))
                    {
                        item.AddError("Value names must be enclosed in quotation marks.");
                    }
                }

                // Unknown references
                foreach (Reference reference in item.References)
                {
                    if (!PredefinedVariables.Variables.Any(v => v.Key.Equals(reference.Value.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        reference.Value.AddError($"The variable \"{reference.Value.Text}\" doens't exist.");
                    }
                }
            }
        }
    }
}
