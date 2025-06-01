namespace SqlParser;

public partial class Parser
{
    private readonly List<Token> _tokens = [];
    private int _position;
    private Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.Eof, "", -1, -1);

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public Parser(Lexer lexer)
    {
        Token token;
        do
        {
            token = lexer.GetNextToken();
            _tokens.Add(token);
        } while (token.Type != TokenType.Eof);
        _position = 0;
    }

    private Token Peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _tokens.Count ? _tokens[index] : new Token(TokenType.Eof, "", -1, -1);
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Current;
    }

    private bool Match(TokenType type, params string[] lexeme)
    {
        if (Current.Type != type) return false;
        return lexeme.Length == 0 || lexeme.Contains(Current.Lexeme);
    }

    private void Expect(TokenType type, params string[] lexemes)
    {
        if (Current.Type != type)
            throw new Exception($"Expected {type}, but found {Current.Type}");
        if (lexemes.Length > 0 && !lexemes.Contains(Current.Lexeme))
            throw new Exception($"Expected one of [{string.Join(", ", lexemes)}], but found '{Current.Lexeme}'");
    }

    private bool IsAtEnd() => _position >= _tokens.Count;

    public Node Parse() => ParseStatement();

    private Node ParseStatement()
    {
        if (Match(TokenType.Keyword, "SELECT"))
            return ParseSelectStatement();
        if (Match(TokenType.Keyword, "DELETE"))
            return ParseDeleteStatement();
        if (Match(TokenType.Keyword, "UPDATE"))
            return ParseUpdateStatement();
        throw new Exception($"Unexpected token: {Current}");
    }

    public void PrintTokens()
    {
        foreach (var token in _tokens)
            Console.WriteLine(token);
        Console.WriteLine();
    }
}
