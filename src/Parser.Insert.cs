namespace SqlParser;

public partial class Parser
{
    private InsertNode ParseInsertStatement()
    {
        Advance(); // consume INSERT
        Expect(TokenType.Keyword, "INTO");
        Advance();
        var table = ParseTable();
        IReadOnlyList<string>? columnNames = null;
        if (Match(TokenType.Punctuation, "("))
        {
            Advance();
            var cols = new List<string>();
            Expect(TokenType.Identifier);
            cols.Add(Current.Lexeme);
            Advance();
            while (Match(TokenType.Punctuation, ","))
            {
                Advance();
                Expect(TokenType.Identifier);
                cols.Add(Current.Lexeme);
                Advance();
            }
            Expect(TokenType.Punctuation, ")");
            Advance();
            columnNames = cols.AsReadOnly();
        }
        Expect(TokenType.Keyword, "VALUES");
        Advance();
        Expect(TokenType.Punctuation, "(");
        Advance();
        var values = new List<ExpressionNode>();
        values.Add(ParsePrimaryExpression());
        while (Match(TokenType.Punctuation, ","))
        {
            Advance();
            values.Add(ParsePrimaryExpression());
        }
        Expect(TokenType.Punctuation, ")");
        Advance();
        return new InsertNode(table, columnNames, values.AsReadOnly());
    }
}
