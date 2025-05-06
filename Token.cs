namespace SqlParser;

public enum TokenType
{
    Keyword,
    Identifier,
    Number,
    String,
    Operator,
    Punctuation,
    Eof,
}

public record Token(TokenType Type, string Value, int Position)
{
    public override string ToString()
    {
        return $"{Type}({Value}) at {Position}";
    }
}