namespace SqlParser;

public partial class Parser
{
    private UpdateNode ParseUpdateStatement()
    {
        Advance(); // consume UPDATE
        var table = ParseTable();
        Expect(TokenType.Keyword, "SET");
        Advance();
        var sets = new List<UpdateSetItem>();
        do
        {
            var column = ParseColumnReference();
            Expect(TokenType.Operator, "=");
            Advance();
            var value = ParsePrimaryExpression();
            sets.Add(new UpdateSetItem(column, value));
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        } while (!IsAtEnd() && Match(TokenType.Punctuation, ",") && Advance() != null);
        WhereNode? where = null;
        if (Match(TokenType.Keyword, "WHERE"))
        {
            Advance();
            where = new WhereNode(ParseJoinExpression()).Balance();
        }
        return new UpdateNode(table, sets.AsReadOnly(), where);
    }
}
