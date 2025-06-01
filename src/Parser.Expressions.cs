namespace SqlParser;

public partial class Parser
{
    private ExpressionNode ParseJoinExpression()
    {
        var left = ParseJoinExpressionTerm();
        while (Match(TokenType.Keyword, "AND", "OR"))
        {
            var op = Current.Lexeme;
            Advance();
            var right = ParseJoinExpressionTerm();
            left = new BinaryExpressionNode(left, op, right);
        }
        return left;
    }

    private ExpressionNode ParseJoinExpressionTerm()
    {
        if (Match(TokenType.Punctuation, "("))
        {
            Advance(); // Consume '('
            var expr = new GroupedExpressionNode(ParseJoinExpression());
            Expect(TokenType.Punctuation, ")");
            Advance(); // Consume ')'
            return expr;
        }
        var leftColumn = ParseColumnReference();
        var op = Current.Lexeme;
        Advance(); // Consume op
        ExpressionNode right = IsValue(Current) ? ParseLiteral() : ParseColumnReference();
        return new BinaryExpressionNode(leftColumn, op, right);
    }

    private ColumnReferenceNode ParseColumnReference()
    {
        if (!Match(TokenType.Identifier))
            throw new Exception($"Expected identifier, but found {Current}");
        var identifier = Current;
        Advance();
        if (Match(TokenType.Punctuation, "."))
        {
            Advance();
            Expect(TokenType.Identifier);
            var columnName = Current.Lexeme;
            Advance();
            return new ColumnReferenceNode(ColumnName: columnName, TableName: identifier.Lexeme);
        }
        return new ColumnReferenceNode(ColumnName: identifier.Lexeme);
    }

    private LiteralNode ParseLiteral()
    {
        switch (Current.Type)
        {
            case TokenType.String:
                var str = Current.Lexeme;
                Advance();
                return new LiteralNode(new String(str));
            case TokenType.Number:
                var num = Current.Lexeme;
                Advance();
                Double.TryParse(num, out var number);
                return new LiteralNode(number);
            default:
                throw new Exception($"Couldn't Parse Token as Literal Node: {Current}");
        }
    }

    private static bool IsValue(Token token) => token.Type is TokenType.String or TokenType.Number;

    internal ExpressionNode ParsePrimaryExpression()
    {
        if (Match(TokenType.Punctuation, "("))
        {
            Advance(); // Consume '('
            var expr = new GroupedExpressionNode(ParseJoinExpression());
            Expect(TokenType.Punctuation, ")");
            Advance(); // Consume ')'
            return expr;
        }
        if (Current.Type == TokenType.String || Current.Type == TokenType.Number)
            return ParseLiteral();
        return ParseColumnReference();
    }
}
