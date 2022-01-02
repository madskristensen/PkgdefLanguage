using System;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using type = Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames;

namespace PkgdefLanguage
{
    public partial class Document
    {
        public bool IsValid { get; private set; }

        private class Errors
        {
            public static Error PL001 { get; } = new("PL001", "Unknown token at this location", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL002 { get; } = new("PL002", "Unclosed registry key entry. Add the missing ] character", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL003 { get; } = new("PL003", "Use the backslash character as delimiter instead of forward slash.", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL004 { get; } = new("PL004", "To set a registry key's default value, use '@' without quotation marks", type.Warning, __VSERRORCATEGORY.EC_WARNING);
            public static Error PL005 { get; } = new("PL005", "Value names must be enclosed in quotation marks.", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL006 { get; } = new("PL006", "The variable \"{0}\" doens't exist.", type.Warning, __VSERRORCATEGORY.EC_WARNING);
            public static Error PL007 { get; } = new("PL007", "Variables must begin and end with $ character.", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL008 { get; } = new("PL008", "This registry key \"{0}\" was already defined earlier in the document", type.Suggestion, __VSERRORCATEGORY.EC_MESSAGE);
        }

        private void AddError(ParseItem item, Error error)
        {
            item.Errors.Add(error);
            IsValid = false;
        }

        private void ValidateDocument()
        {
            IsValid = true;

            foreach (ParseItem item in Items)
            {
                // Unknown symbols
                if (item.Type == ItemType.Unknown)
                {
                    AddError(item, Errors.PL001);
                }

                // Registry key
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]"))
                    {
                        AddError(item, Errors.PL002);
                    }
                    else if (trimmedText.Contains("/") && !trimmedText.Contains("\\/"))
                    {
                        AddError(item, Errors.PL003);
                    }

                    var index = Items.IndexOf(item);
                    if (Items.Take(index).Any(i => i.Type == ItemType.RegistryKey && item.Text == i.Text))
                    {
                        AddError(item, Errors.PL008.WithFormat(trimmedText));
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
                            AddError(name, Errors.PL004);
                        }
                    }
                    else if (name?.Type == ItemType.Literal && name?.Text != "@")
                    {
                        AddError(name, Errors.PL005);
                    }
                }

                // Make sure strings are correctly closed with quotation mark
                if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\""))
                    {
                        AddError(item, Errors.PL005);
                    }
                }

                // References
                foreach (ParseItem reference in item.References)
                {
                    var refTrim = reference.Text.Trim();

                    if (!refTrim.EndsWith("$"))
                    {
                        AddError(reference, Errors.PL007);
                    }
                    else if (!PredefinedVariables.Variables.Any(v => v.Key.Equals(refTrim.Trim('$'), StringComparison.OrdinalIgnoreCase)))
                    {
                        AddError(reference, Errors.PL006.WithFormat(refTrim));
                    }
                }
            }
        }
    }
}
