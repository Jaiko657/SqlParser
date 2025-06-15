namespace SqlParser;

public partial class Parser
{
    private GroupByNode? ParseGroupBy()
    {
        if (!Match(TokenType.Keyword, "GROUP")) return null;
        Advance();
        Expect(TokenType.Keyword, "BY");
        Advance(); // consume BY
        var columns = new List<ColumnNode>();
        do
        {
            columns.Add(ParseColumn());
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        } while (!IsAtEnd() && Match(TokenType.Punctuation, ",") && Advance() != null);
        return new GroupByNode(columns.AsReadOnly());
    }

    private HavingNode? ParseHaving()
    {
        if (!Match(TokenType.Keyword, "HAVING")) return null;
        Advance();
        return new HavingNode(ParseJoinExpression()).Balance();
    }
}
