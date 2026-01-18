using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PkgdefLanguage
{
    public partial class Document
    {
        private static readonly Regex _regexProperty = new(@"^(?<name>""[^""]+""|@)(\s)*(?<equals>=)\s*(?<value>((dword:|qword:|hex).+|"".+))", RegexOptions.Compiled);
        private static readonly Regex _regexRef = new(@"\$[\w]+\$?", RegexOptions.Compiled);

        // Pre-allocate reusable collections to reduce GC pressure
        private readonly List<ParseItem> _tempItems = new(256);
        private readonly List<ParseItem> _tempReferences = new(16);
        private readonly List<ParseItem> _tempLineItems = new(4);

        public void Parse()
        {
            var start = 0;

            // Reuse the temporary list instead of creating new ones
            _tempItems.Clear();

            foreach (var line in _lines)
            {
                IEnumerable<ParseItem> current = ParseLine(start, line, _tempItems);
                _tempItems.AddRange(current);
                start += line.Length;
            }

            // Create a new list with the exact capacity needed
            Items = new List<ParseItem>(_tempItems.Count);
            Items.AddRange(_tempItems);

            // Cache indices in each ParseItem to avoid O(n) IndexOf lookups
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i]._cachedIndex = i;
            }
        }

        private Entry _currentEntry = null;

        private IEnumerable<ParseItem> ParseLine(int start, string line, List<ParseItem> tokens)
        {
            var trimmedLine = line.Trim();

            // Reuse the temporary list instead of allocating a new one
            _tempLineItems.Clear();

            // Comment
            if (trimmedLine.StartsWith(Constants.CommentChars[0], StringComparison.Ordinal) || trimmedLine.StartsWith(Constants.CommentChars[1], StringComparison.Ordinal))
            {
                _tempLineItems.Add(ToParseItem(line, start, ItemType.Comment, false));
            }
            // Preprocessor
            else if (trimmedLine.StartsWith("#include", StringComparison.Ordinal))
            {
                _tempLineItems.Add(ToParseItem(line, start, ItemType.Preprocessor, false));
            }
            // Registry key
            else if (trimmedLine.StartsWith("[", StringComparison.Ordinal))
            {
                var key = new ParseItem(start, line, this, ItemType.RegistryKey);
                _currentEntry = new Entry(key, this);
                _tempLineItems.Add(_currentEntry);
                _tempLineItems.Add(key);
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

                _tempLineItems.Add(name);
                _tempLineItems.Add(equals);
                _tempLineItems.Add(value);
            }
            // Incomplete property (just property name being typed, no = yet)
            else if (tokens.Count > 0 && _currentEntry != null && (trimmedLine.StartsWith("\"", StringComparison.Ordinal) || trimmedLine == "@"))
            {
                // Colorize the property name even if = hasn't been added yet
                ItemType type = trimmedLine.StartsWith("\"", StringComparison.Ordinal) ? ItemType.String : ItemType.Literal;
                _tempLineItems.Add(ToParseItem(line, start, type, false));
            }
            // Unknown
            else if (trimmedLine.Length > 0)
            {
                // Check for line splits which is a line ending with a backslash
                var lineSplit = tokens.LastOrDefault()?.Text.TrimEnd().EndsWith("\\", StringComparison.Ordinal) == true;
                ItemType type = lineSplit ? tokens.Last().Type : ItemType.Unknown;
                _tempLineItems.Add(new ParseItem(start, line, this, type));
            }

            return _tempLineItems;
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
            ItemType type = group.Value.StartsWith("\"", StringComparison.Ordinal) ? ItemType.String : ItemType.Literal;
            return ToParseItem(group.Value, start + group.Index, type, supportsVariableReferences);
        }

        private void AddVariableReferences(ParseItem token)
        {
            // Clear and reuse the temporary references list
            _tempReferences.Clear();
            
            foreach (Match match in _regexRef.Matches(token.Text))
            {
                ParseItem reference = ToParseItem(match.Value, token.Span.Start + match.Index, ItemType.Reference, false);
                _tempReferences.Add(reference);
            }
            
            // Only allocate the actual references list if we have references
            if (_tempReferences.Count > 0)
            {
                token.References.AddRange(_tempReferences);
            }
        }
    }
}
