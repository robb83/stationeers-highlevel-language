namespace Stationeers.Compiler
{
    public class Token
    {
        public TokenType Type;
        public string Value;
        public int Begin;
        public int End;

        public Token(TokenType type, string value, int begin, int end)
        {
            Type = type;
            Value = value;
            Begin = begin;
            End = end;
        }

        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }
}
