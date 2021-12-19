﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private static readonly Regex _regexProperty = new(@"^(?<name>.+)(\s)*(?<equals>=)\s*(?<value>.+)", RegexOptions.Compiled);
        private static readonly Regex _regexRef = new(@"(?<open>\$)(?<value>[\w]+)(?<close>\$)", RegexOptions.Compiled);

        public bool IsParsing { get; private set; }

        public Task ParseAsync()
        {
            IsParsing = true;

            return Task.Run(() =>
            {
                var start = 0;

                try
                {
                    List<ParseItem> tokens = new();

                    foreach (var line in _lines)
                    {
                        IEnumerable<ParseItem> current = ParseLine(start, line, tokens);

                        if (current != null)
                        {
                            tokens.AddRange(current);
                        }

                        start += line.Length;
                    }

                    Items = tokens;

                    OrganizeItems();
                    ValidateDocument();
                }
                finally
                {
                    IsParsing = false;
                    Parsed?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private IEnumerable<ParseItem> ParseLine(int start, string line, List<ParseItem> tokens)
        {
            var trimmedLine = line.Trim();
            List<ParseItem> items = new();

            // Comment
            if (trimmedLine.StartsWith(Constants.CommentChar.ToString()))
            {
                items.Add(ToParseItem(line, start, ItemType.Comment, false));
            }
            // Empty line
            else if (string.IsNullOrWhiteSpace(line))
            {
                items.Add(ToParseItem(line, start, ItemType.EmptyLine, false));
            }
            // Registry key
            else if (trimmedLine.StartsWith("[", StringComparison.Ordinal))
            {
                var key = new ParseItem(start, line, this, ItemType.RegistryKey);
                items.Add(key);
                AddVariableReferences(key);
            }
            // Property
            else if (tokens.Count > 0 && IsMatch(_regexProperty, trimmedLine, out Match matchHeader))
            {
                items.Add(ToParseItem(matchHeader, start, "name"));
                items.Add(ToParseItem(matchHeader, start, "value", true));
            }
            // Unknown
            else
            {
                items.Add(new ParseItem(start, line, this, ItemType.Unknown));
            }

            return items;
        }

        public static bool IsMatch(Regex regex, string line, out Match match)
        {
            match = regex.Match(line);
            return match.Success;
        }

        private ParseItem ToParseItem(string line, int start, ItemType type, bool supportsVariableReferences = true)
        {
            var item = new ParseItem(start, line, this, type);

            if (supportsVariableReferences)
            {
                AddVariableReferences(item);
            }

            return item;
        }

        private ParseItem ToParseItem(Match match, int start, string groupName, ItemType type, bool supportsVariableReferences = true)
        {
            Group group = match.Groups[groupName];
            return ToParseItem(group.Value, start + group.Index, type, supportsVariableReferences);
        }

        private ParseItem ToParseItem(Match match, int start, string groupName, bool supportsVariableReferences = true)
        {
            Group group = match.Groups[groupName];
            ItemType type = group.Value.StartsWith("\"") ? ItemType.PropertyName : ItemType.PropertyValue;
            return ToParseItem(group.Value, start + group.Index, type, supportsVariableReferences);
        }

        private void AddVariableReferences(ParseItem token)
        {
            foreach (Match match in _regexRef.Matches(token.Text))
            {
                ParseItem open = ToParseItem(match, token.Start, "open", ItemType.ReferenceBraces, false);
                ParseItem value = ToParseItem(match, token.Start, "value", ItemType.ReferenceName, false);
                ParseItem close = ToParseItem(match, token.Start, "close", ItemType.ReferenceBraces, false);

                var reference = new Reference(open, value, close);

                token.References.Add(reference);
            }
        }

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

                // Unknown variables
                if (item.Type == ItemType.ReferenceName)
                {
                    if (!CompletionCatalog.Variables.Any(v => v.Key.Equals(item.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        item.Errors.Add($"The variable \"{item.Text}\" doens't exist.");
                    }
                }
            }
        }

        private void OrganizeItems()
        {
            List<Entry> entries = new();
            Entry currentEntry = null;

            foreach (ParseItem item in Items)
            {
                if (item.Type == ItemType.RegistryKey)
                {
                    currentEntry = new Entry(item);
                    entries.Add(currentEntry);
                }
                else if (item.Type == ItemType.PropertyName)
                {
                    var property = new Property(item, item.Next);
                    currentEntry?.Properties.Add(property);
                }
            }

            Entries = entries;
        }

        public event EventHandler Parsed;
    }
}