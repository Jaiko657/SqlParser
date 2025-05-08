namespace SqlParser;

public enum TokenType
{
    Keyword,
    Identifier,
    Number,
    String,
    Operator,
    Punctuation,
    Eof
}

public record Token(TokenType Type, string Lexeme, int StartPosition, int EndPosition)
{
    public int Length => EndPosition - StartPosition + 1;
    public override string ToString()
    {
        if (Lexeme.Length <= 1) 
            return $"{Type}({Lexeme}) {StartPosition}";
        return $"{Type}({Lexeme}) at {StartPosition}-{EndPosition}";
    }
}