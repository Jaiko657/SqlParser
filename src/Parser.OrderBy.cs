namespace SqlParser;

public partial class Parser
{
    private OrderByNode? ParseOrderBy()
    {
        if (!Match(TokenType.Keyword, "ORDER")) return null;
        Advance();
        Expect(TokenType.Keyword, "BY");
        Advance(); // consume BY
        var items = new List<OrderByItem>();
        do
        {
            items.Add(ParseOrderByItem());
        } while (!IsAtEnd() && Match(TokenType.Punctuation, ",") && Advance() != null);
        return new OrderByNode(items.AsReadOnly());
    }

    private OrderByItem ParseOrderByItem()
    {
        var column = ParseColumn();
        var direction = SortDirection.Ascending;
        if ((Match(TokenType.Keyword, "ASC", "DESC") || (Current.Type == TokenType.Identifier && (Current.Lexeme.Equals("ASC", StringComparison.OrdinalIgnoreCase) || Current.Lexeme.Equals("DESC", StringComparison.OrdinalIgnoreCase)))))
        {
            direction = Current.Lexeme.Equals("DESC", StringComparison.OrdinalIgnoreCase) ? SortDirection.Descending : SortDirection.Ascending;
            Advance();
        }
        return new OrderByItem(column, direction);
    }
}
