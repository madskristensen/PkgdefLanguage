using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace PkgdefLanguage
{
    public class Error
    {
        public Error(string errorCode, string message, ErrorSeverity severity)
        {
            ErrorCode = errorCode;
            Message = message;
            Severity = severity;
        }

        public string ErrorCode { get; }
        public string Message { get; private set; }
        public ErrorSeverity Severity { get; }

        public Error WithFormat(params string[] replacements)
        {
            Message = string.Format(Message, replacements);
            return this;
        }
    }

    public enum ErrorSeverity
    {
        Message,
        Warning,
        Error
    }

    public class ParseItem
    {
        public HashSet<Error> _errors = new();

        public ParseItem(int start, string text, Document document, ItemType type)
        {
            Text = text;
            Document = document;
            Type = type;
            Span = new Span(start, Text.Length);
        }

        public List<ParseItem> Children = new();

        public ItemType Type { get; }

        public virtual Span Span { get; }

        public virtual string Text { get; }

        public Document Document { get; }

        public List<Reference> References { get; } = new();

        public IEnumerable<Error> Errors => _errors;

        public bool IsValid => _errors.Count == 0;

        public void AddError(Error error)
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
            return parseItem.Span;
        }

        public override string ToString()
        {
            return Type + " " + Text;
        }

        public override int GetHashCode()
        {
            var hashCode = -1393027003;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + Span.Start.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is ParseItem item &&
                   Type == item.Type &&
                   EqualityComparer<Span>.Default.Equals(Span, item.Span) &&
                   Text == item.Text;
        }
    }
}
