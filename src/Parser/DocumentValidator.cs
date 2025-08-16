using System;
using System.Collections.Generic;
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
            public static Error PL006 { get; } = new("PL006", "The variable \"{0}\" doesn't exist.", type.Warning, __VSERRORCATEGORY.EC_WARNING);
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

            // Pre-create sets for faster lookups
            var registryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var predefinedVariables = new HashSet<string>(
                PredefinedVariables.Variables.Keys,
                StringComparer.OrdinalIgnoreCase);

            // Single pass validation with optimized lookups
            for (int i = 0; i < Items.Count; i++)
            {
                ParseItem item = Items[i];

                // Unknown symbols
                if (item.Type == ItemType.Unknown)
                {
                    AddError(item, Errors.PL001);
                    continue;
                }

                // Registry key validation
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]"))
                    {
                        AddError(item, Errors.PL002);
                    }

                    // Check for duplicate registry keys using HashSet for O(1) lookup
                    if (!registryKeys.Add(trimmedText))
                    {
                        AddError(item, Errors.PL008.WithFormat(trimmedText));
                    }
                }
                // Property validation
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
                // String validation
                else if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\""))
                    {
                        AddError(item, Errors.PL005);
                    }
                }

                // Reference validation - batch process for better performance
                if (item.References.Count > 0)
                {
                    ValidateReferences(item, predefinedVariables);
                }
            }
        }

        private void ValidateReferences(ParseItem item, HashSet<string> predefinedVariables)
        {
            foreach (ParseItem reference in item.References)
            {
                var refTrim = reference.Text.Trim();

                if (refTrim.EndsWith("$"))
                {
                    var variableName = refTrim.Trim('$');
                    if (!predefinedVariables.Contains(variableName))
                    {
                        AddError(reference, Errors.PL006.WithFormat(refTrim));
                    }
                }
            }
        }
    }
}
