namespace PkgdefLanguage
{
    public class Property
    {
        public Property(ParseItem name, ParseItem value)
        {
            Name = name;
            Value = value;
        }

        public ParseItem Name { get; }
        public ParseItem Value { get; }
    }
}
