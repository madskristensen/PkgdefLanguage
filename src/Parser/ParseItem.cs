using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class ParseItem
    {
        public List<string> _errors = new();
        public ParseItem(int start, string text, Document document, ItemType type)
        {
            Start = start;
            Text = text;
            Document = document;
            Type = type;
        }

        public ItemType Type { get; }

        public int Start { get; }

        public virtual string Text { get; protected set; }

        public Document Document { get; }

        public virtual int End => Start + Text.Length;

        public virtual int Length => End - Start;

        public List<Reference> References { get; } = new List<Reference>();

        public IEnumerable<string> Errors => _errors;

        public bool IsValid => _errors.Count == 0;

        public virtual bool Contains(int position)
        {
            return Start <= position && End >= position;
        }

        public void AddError(string error)
        {
            Document.IsValid = false;
            _errors.Add(error);
        }

        public ParseItem Previous
        {
            get
            {
                var index = Document.Items.IndexOf(this);
                return index > 0 ? Document.Items[index - 1] : null;
            }
        }

        public ParseItem Next
        {
            get
            {
                var index = Document.Items.IndexOf(this);
                return Document.Items.ElementAtOrDefault(index + 1);
            }
        }

        public static implicit operator Span(ParseItem parseItem)
        {
            return Span.FromBounds(parseItem.Start, parseItem.End);
        }

        public override string ToString()
        {
            return Type + " " + Text;
        }
    }
}
