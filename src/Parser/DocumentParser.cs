using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private static readonly Regex _regexProperty = new(@"^(?<name>.+)(\s)*(?<equals>=)\s*(?<value>.+)", RegexOptions.Compiled);
        private static readonly Regex _regexRef = new(@"(?<open>\$)(?<value>[\w]+)(?<close>\$)", RegexOptions.Compiled);

        public void Parse()
        {
            var start = 0;

            List<ParseItem> items = new();

            foreach (var line in _lines)
            {
                IEnumerable<ParseItem> current = ParseLine(start, line, items);
                items.AddRange(current);
                start += line.Length;
            }

            Items = items;
        }

        private Entry _currentEntry = null;

        private IEnumerable<ParseItem> ParseLine(int start, string line, List<ParseItem> tokens)
        {
            var trimmedLine = line.TrimEnd();
            List<ParseItem> items = new();

            // Comment
            if (trimmedLine.StartsWith(Constants.CommentChars[0]) || trimmedLine.StartsWith(Constants.CommentChars[1]))
            {
                items.Add(ToParseItem(line, start, ItemType.Comment, false));
            }
            // Registry key
            else if (trimmedLine.StartsWith("[", StringComparison.Ordinal))
            {
                var key = new ParseItem(start, line, this, ItemType.RegistryKey);
                _currentEntry = new Entry(key, this);
                items.Add(_currentEntry);
                items.Add(key);
                AddVariableReferences(key);
            }
            // Property
            else if (tokens.Count > 0 && IsMatch(_regexProperty, trimmedLine, out Match matchHeader))
            {
                ParseItem name = ToParseItem(matchHeader, start, "name", false);
                ParseItem equals = ToParseItem(matchHeader, start, "equals", ItemType.Operator, false);
                ParseItem value = ToParseItem(matchHeader, start, "value");

                if (_currentEntry != null)
                {
                    var prop = new Property(name, value);
                    _currentEntry.Properties.Add(prop);
                }

                items.Add(name);
                items.Add(equals);
                items.Add(value);
            }
            // Unknown
            else if (trimmedLine.Length > 0)
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
            ItemType type = group.Value.StartsWith("\"") ? ItemType.String : ItemType.Literal;
            return ToParseItem(group.Value, start + group.Index, type, supportsVariableReferences);
        }

        private void AddVariableReferences(ParseItem token)
        {
            foreach (Match match in _regexRef.Matches(token.Text))
            {
                ParseItem open = ToParseItem(match, token.Span.Start, "open", ItemType.ReferenceBraces, false);
                ParseItem value = ToParseItem(match, token.Span.Start, "value", ItemType.ReferenceName, false);
                ParseItem close = ToParseItem(match, token.Span.Start, "close", ItemType.ReferenceBraces, false);

                var reference = new Reference(open, value, close);

                token.References.Add(reference);
            }
        }
    }
}
