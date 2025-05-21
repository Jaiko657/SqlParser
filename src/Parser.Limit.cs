namespace SqlParser;

public partial class Parser
{
    private LimitNode? ParseLimit()
    {
        if (!Match(TokenType.Keyword, "LIMIT")) return null;
        Advance();
        Expect(TokenType.Number);
        var limit = int.Parse(Current.Lexeme);
        Advance();
        int? offset = null;
        if (Match(TokenType.Keyword, "OFFSET"))
        {
            Advance();
            Expect(TokenType.Number);
            offset = int.Parse(Current.Lexeme);
            Advance();
        }
        return new LimitNode(limit, offset);
    }
}
