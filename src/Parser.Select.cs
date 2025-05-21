namespace SqlParser;

public partial class Parser
{
    private SelectNode ParseSelectStatement()
    {
        Advance(); // Skip SELECT
        var columns = ParseColumns();
        Expect(TokenType.Keyword, "FROM");
        Advance();
        var fromTable = ParseTable();

        if (IsAtEnd())
            return new SelectNode(Columns: columns, FromTable: fromTable, Joins: []);

        if (!Match(TokenType.Keyword))
            throw new Exception($"Expected Keyword, but found {Current}");

        var joins = ParseJoins();
        WhereNode? where = null;
        if (Match(TokenType.Keyword, "WHERE"))
        {
            Advance();
            where = new WhereNode(ParseJoinExpression()).Balance();
        }
        GroupByNode? groupBy = null;
        HavingNode? having = null;
        OrderByNode? orderBy = ParseOrderBy();
        LimitNode? limit = ParseLimit();

        return new SelectNode(
            Columns: columns,
            FromTable: fromTable,
            Joins: joins.AsReadOnly(),
            Where: where,
            GroupBy: groupBy,
            Having: having,
            OrderBy: orderBy,
            Limit: limit);
    }

    private IList<JoinNode> ParseJoins()
    {
        IList<JoinNode> joins = [];
        while (Match(TokenType.Keyword, "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "LEFT OUTER JOIN",
                   "RIGHT OUTER JOIN", "CROSS JOIN"))
        {
            var joinType = Current.Lexeme switch
            {
                "JOIN" or "INNER JOIN" => JoinType.Inner,
                "LEFT JOIN" or "LEFT OUTER JOIN" => JoinType.Left,
                "RIGHT JOIN" or "RIGHT OUTER JOIN" => JoinType.Right,
                "FULL JOIN" => JoinType.Full,
                "CROSS JOIN" => JoinType.Cross,
                _ => throw new Exception($"Unexpected join type: {Current.Lexeme}")
            };

            var tableName = Advance().Lexeme;
            Expect(TokenType.Identifier);
            var table = new TableNode(tableName);
            Advance();
            if (!Match(TokenType.Keyword, "ON") && Match(TokenType.Identifier))
            {
                var alias = new AliasNode(Current.Lexeme);
                table = table with { Alias = alias };
                Advance(); // consume alias so next token is ON
            }

            if (joinType == JoinType.Cross)
            {
                joins.Add(new JoinNode(joinType, table, null));
                continue;
            }

            Expect(TokenType.Keyword, "ON");
            Advance(); // consume ON
            var condition = ParseJoinExpression();
            joins.Add(new JoinNode(joinType, table, condition).Balance());
        }
        return joins;
    }

    private List<ColumnNode> ParseColumns()
    {
        List<ColumnNode> columns = [];
        do
        {
            columns.Add(ParseColumn());
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        } while (!IsAtEnd() && Match(TokenType.Punctuation, ",") && Advance() != null);
        return columns;
    }

    private ColumnNode ParseColumn()
    {
        if (Match(TokenType.Operator, "*"))
        {
            var token = Current;
            Advance();
            return new ColumnNode(token.Lexeme);
        }
        if (!Match(TokenType.Identifier))
            throw new Exception($"Expected identifier or '*', but found {Current}");

        var identifier = Current;
        Advance();
        if (Match(TokenType.Punctuation, "."))
        {
            Advance();
            if (Match(TokenType.Operator, "*"))
            {
                Advance();
                return new ColumnNode(ColumnName: $"{identifier.Lexeme}.*");
            }
            if (Match(TokenType.Identifier))
            {
                var columnName = Current.Lexeme;
                Advance();
                return new ColumnNode(ColumnName: $"{identifier.Lexeme}.{columnName}", Alias: ParsePossibleAlias());
            }
        }
        else
        {
            return new ColumnNode(ColumnName: identifier.Lexeme, Alias: ParsePossibleAlias());
        }
        throw new Exception($"Unexpected token: {Current}");
    }

    private AliasNode? ParsePossibleAlias()
    {
        if (!Match(TokenType.Keyword, "AS")) return null;
        Advance(); // consume 'as'
        Expect(TokenType.Identifier);
        var ret = new AliasNode(Current.Lexeme);
        Advance();
        return ret;
    }

    private TableNode ParseTable()
    {
        Expect(TokenType.Identifier);
        var tableName = Current.Lexeme;
        if (Peek(1).Lexeme == "." && Peek(2).Type == TokenType.Identifier)
        {
            Advance(); // consume '.'
            tableName = tableName + "." + Advance().Lexeme;
        }
        Advance();
        AliasNode? alias = null;
        if (Match(TokenType.Identifier))
        {
            alias = new AliasNode(Current.Lexeme);
            Advance();
        }
        return new TableNode(TableName: tableName, Alias: alias);
    }
}
