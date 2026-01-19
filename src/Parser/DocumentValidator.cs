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
            public static Error PL009 { get; } = new("PL009", "Invalid dword value. Must be 8 hexadecimal characters (0-9, A-F).", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL010 { get; } = new("PL010", "Invalid qword value. Must be 16 hexadecimal characters (0-9, A-F).", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
            public static Error PL011 { get; } = new("PL011", "Invalid hex value. Must be comma-separated hexadecimal bytes (00-FF).", type.SyntaxError, __VSERRORCATEGORY.EC_ERROR);
        }

        private void AddError(ParseItem item, Error error)
        {
                item.Errors.Add(error);
                IsValid = false;
            }

            protected void ValidateDocument()
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

                // Unknown symbols - check if it's an unquoted property name
                if (item.Type == ItemType.Unknown)
                {
                    // Check if this unknown token is actually an unquoted property name
                    // by seeing if the next item is an Operator (=)
                    if (item.Next?.Type == ItemType.Operator)
                    {
                        var trimmedText = item.Text.Trim();
                        if (trimmedText != "@")
                        {
                            AddError(item, Errors.PL005);
                        }
                    }
                    else
                    {
                        AddError(item, Errors.PL001);
                    }
                    continue;
                }

                // Registry key validation
                if (item.Type == ItemType.RegistryKey)
                {
                    var trimmedText = item.Text.Trim();

                    if (!trimmedText.EndsWith("]", StringComparison.Ordinal))
                    {
                        AddError(item, Errors.PL002);
                    }

                    // Check for forward slashes in registry path
                    if (trimmedText.Contains('/'))
                    {
                        AddError(item, Errors.PL003);
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
                        var trimmedName = name.Text.Trim();
                        if (trimmedName == "\"@\"")
                        {
                            AddError(name, Errors.PL004);
                        }
                    }
                    else if (name?.Type == ItemType.Literal)
                    {
                        var trimmedName = name.Text.Trim();
                        if (trimmedName != "@")
                        {
                            AddError(name, Errors.PL005);
                        }
                    }

                    // Validate property values (dword, qword, hex)
                    if (value != null)
                    {
                        ValidatePropertyValue(value);
                    }
                }
                // String validation
                else if (item.Type == ItemType.String)
                {
                    if (!item.Text.EndsWith("\"", StringComparison.Ordinal))
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

                // Check if variable is properly closed with $
                if (!refTrim.EndsWith("$", StringComparison.Ordinal))
                {
                    // PL007: Variable missing closing $
                    AddError(reference, Errors.PL007);
                }
                else
                {
                    // Variable is properly formatted, check if it exists
                    var variableName = refTrim.Trim('$');
                    if (!predefinedVariables.Contains(variableName))
                    {
                        AddError(reference, Errors.PL006.WithFormat(refTrim));
                    }
                }
            }
        }

        private void ValidatePropertyValue(ParseItem value)
        {
            var trimmedValue = value.Text.Trim();

            // Validate dword: values
            if (trimmedValue.StartsWith("dword:", StringComparison.OrdinalIgnoreCase))
            {
                var hexPart = trimmedValue.Substring(6); // Remove "dword:" prefix
                if (!IsValidHexValue(hexPart, 8))
                {
                    AddError(value, Errors.PL009);
                }
            }
            // Validate qword: values
            else if (trimmedValue.StartsWith("qword:", StringComparison.OrdinalIgnoreCase))
            {
                var hexPart = trimmedValue.Substring(6); // Remove "qword:" prefix
                if (!IsValidHexValue(hexPart, 16))
                {
                    AddError(value, Errors.PL010);
                }
            }
            // Validate hex(X): values
            else if (trimmedValue.StartsWith("hex", StringComparison.OrdinalIgnoreCase))
            {
                var colonIndex = trimmedValue.IndexOf(':');
                if (colonIndex > 0)
                {
                    var hexPart = trimmedValue.Substring(colonIndex + 1); // Remove "hex(X):" prefix
                    if (!IsValidHexArrayValue(hexPart))
                    {
                        AddError(value, Errors.PL011);
                    }
                }
            }
        }

        private bool IsValidHexValue(string value, int expectedLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != expectedLength)
            {
                return false;
            }

            // Check if all characters are valid hex digits (0-9, A-F, a-f)
            foreach (char c in value)
            {
                if (!IsHexDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsValidHexArrayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Split by comma and validate each byte
            var bytes = value.Split(',');
            foreach (var byteStr in bytes)
            {
                var trimmedByte = byteStr.Trim();

                // Each byte should be exactly 2 hex characters
                if (trimmedByte.Length != 2)
                {
                    return false;
                }

                foreach (char c in trimmedByte)
                {
                    if (!IsHexDigit(c))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || 
                   (c >= 'A' && c <= 'F') || 
                   (c >= 'a' && c <= 'f');
        }
    }
}
