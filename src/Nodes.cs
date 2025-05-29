namespace SqlParser;

public interface ISqlNodeVisitor
{
    void Visit(Node node);
}

public record Node
{
    public void Accept(ISqlNodeVisitor visitor)
    {
        visitor.Visit(this);
    }
}

public record SelectNode(
    IReadOnlyList<ColumnNode> Columns,
    TableNode FromTable,
    IReadOnlyList<JoinNode> Joins,
    WhereNode? Where = null,
    GroupByNode? GroupBy = null,
    HavingNode? Having = null,
    OrderByNode? OrderBy = null,
    LimitNode? Limit = null
) : Node;

public record ColumnNode(
    string ColumnName,
    AliasNode? Alias = null
) : Node;

public record TableNode(
    string TableName,
    AliasNode? Alias = null
) : Node;

public record AliasNode(
    string AliasName
) : Node;

public record WhereNode(
    ExpressionNode Condition
) : Node;

public record JoinNode(
    JoinType Type,
    TableNode Table,
    ExpressionNode? Condition
) : Node;

public enum JoinType
{
    Inner,
    Left,
    Right,
    Full,
    Cross
}

public record GroupByNode(
    IReadOnlyList<ColumnNode> Columns
) : Node;

public record HavingNode(
    ExpressionNode Condition
) : Node;

public record OrderByNode(
    IReadOnlyList<OrderByItem> Items
) : Node;

public record OrderByItem(
    ColumnNode Column,
    SortDirection Direction
);

public enum SortDirection
{
    Ascending,
    Descending
}

public record LimitNode(
    int Limit,
    int? Offset = null
) : Node;

public abstract record ExpressionNode : Node;

public record GroupedExpressionNode(ExpressionNode InnerExpression): ExpressionNode;

public record BinaryExpressionNode(
    ExpressionNode Left,
    string Operator,
    ExpressionNode Right
) : ExpressionNode;

public record ColumnReferenceNode(
    string ColumnName,
    string? TableName = null
) : ExpressionNode;

public record LiteralNode(
    object Value
) : ExpressionNode;

public record FunctionNode(
    string FunctionName,
    IReadOnlyList<ExpressionNode> Arguments
) : ExpressionNode;

public record SubqueryNode(
    SelectNode Select
) : ExpressionNode;

public record UnionNode(
    SelectNode Left,
    SelectNode Right,
    bool IsUnionAll = false
) : Node;

public record DeleteNode(
    TableNode Table,
    WhereNode? Where = null
) : Node;