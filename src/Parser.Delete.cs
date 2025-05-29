namespace SqlParser;

public partial class Parser
{
    private DeleteNode ParseDeleteStatement()
    {
        Advance(); // consume DELETE
        Expect(TokenType.Keyword, "FROM");
        Advance();
        var table = ParseTable();
        WhereNode? where = null;
        if (Match(TokenType.Keyword, "WHERE"))
        {
            Advance();
            where = new WhereNode(ParseJoinExpression()).Balance();
        }
        return new DeleteNode(table, where);
    }
}
