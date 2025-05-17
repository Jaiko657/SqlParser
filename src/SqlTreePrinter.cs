using System.Text;

namespace SqlParser;

public class TreePrinter : ISqlNodeVisitor
{
    private readonly StringBuilder _sb = new();
    private int _depth = 0;

    public string Print(Node node)
    {
        _sb.Clear();
        _depth = 0;
        Visit(node);
        return _sb.ToString();
    }

    public void Reset()
    {
        _sb.Clear();
        _depth = 0;
    }
    
    public void Visit(Node node)
    {
        switch (node)
        {
            case SelectNode selectNode:
                PrintSelectNode(selectNode);
                break;
            case ColumnNode columnNode:
                PrintColumnNode(columnNode);
                break;
            case TableNode tableNode:
                PrintTableNode(tableNode);
                break;
            case AliasNode aliasNode:
                PrintAliasNode(aliasNode);
                break;
            case WhereNode whereNode:
                PrintWhereNode(whereNode);
                break;
            case JoinNode joinNode:
                PrintJoinNode(joinNode);
                break;
            case GroupByNode groupByNode:
                PrintGroupByNode(groupByNode);
                break;
            case HavingNode havingNode:
                PrintHavingNode(havingNode);
                break;
            case OrderByNode orderByNode:
                PrintOrderByNode(orderByNode);
                break;
            case LimitNode limitNode:
                PrintLimitNode(limitNode);
                break;
            case GroupedExpressionNode groupedExpressionNode:
                PrintGroupedExpressionNode(groupedExpressionNode);
                break;
            case BinaryExpressionNode binaryExpressionNode:
                PrintBinaryExpressionNode(binaryExpressionNode);
                break;
            case ColumnReferenceNode columnReferenceNode:
                PrintColumnReferenceNode(columnReferenceNode);
                break;
            case LiteralNode literalNode:
                PrintLiteralNode(literalNode);
                break;
            case FunctionNode functionNode:
                PrintFunctionNode(functionNode);
                break;
            case SubqueryNode subqueryNode:
                PrintSubqueryNode(subqueryNode);
                break;
            case UnionNode unionNode:
                PrintUnionNode(unionNode);
                break;
            default:
                AppendLine($"Unknown node type: {node.GetType().Name}");
                break;
        }
    }

    private void PrintSelectNode(SelectNode node)
    {
        AppendLine("SelectNode");
        _depth++;
        foreach (var column in node.Columns)
        {
            Visit(column);
        }
        Visit(node.FromTable);
        foreach (var join in node.Joins)
        {
            Visit(join);
        }
        if (node.Where != null) Visit(node.Where);
        if (node.GroupBy != null) Visit(node.GroupBy);
        if (node.Having != null) Visit(node.Having);
        if (node.OrderBy != null) Visit(node.OrderBy);
        if (node.Limit != null) Visit(node.Limit);
        _depth--;
    }

    private void PrintColumnNode(ColumnNode node)
    {
        AppendLine($"ColumnNode: {node.ColumnName}");
        if (node.Alias != null)
        {
            _depth++;
            Visit(node.Alias);
            _depth--;
        }
    }

    private void PrintTableNode(TableNode node)
    {
        AppendLine($"TableNode: {node.TableName}");
        if (node.Alias != null)
        {
            _depth++;
            Visit(node.Alias);
            _depth--;
        }
    }

    private void PrintAliasNode(AliasNode node)
    {
        AppendLine($"AliasNode: {node.AliasName}");
    }

    private void PrintWhereNode(WhereNode node)
    {
        AppendLine("WhereNode");
        _depth++;
        Visit(node.Condition);
        _depth--;
    }

    private void PrintJoinNode(JoinNode node)
    {
        AppendLine($"JoinNode: {node.Type}");
        _depth++;
        Visit(node.Table);
        if (node.Condition != null) Visit(node.Condition);
        _depth--;
    }

    private void PrintGroupByNode(GroupByNode node)
    {
        AppendLine("GroupByNode");
        _depth++;
        foreach (var column in node.Columns)
        {
            Visit(column);
        }
        _depth--;
    }

    private void PrintHavingNode(HavingNode node)
    {
        AppendLine("HavingNode");
        _depth++;
        Visit(node.Condition);
        _depth--;
    }

    private void PrintOrderByNode(OrderByNode node)
    {
        AppendLine("OrderByNode");
        _depth++;
        foreach (var item in node.Items)
        {
            AppendLine($"OrderByItem: {item.Direction}");
            _depth++;
            Visit(item.Column);
            _depth--;
        }
        _depth--;
    }

    private void PrintLimitNode(LimitNode node)
    {
        AppendLine($"LimitNode: {node.Limit}" + (node.Offset.HasValue ? $", Offset: {node.Offset}" : ""));
    }

    private void PrintGroupedExpressionNode(GroupedExpressionNode node)
    {
        AppendLine($"GroupedExpressionNode");
        _depth++;
        Visit(node.InnerExpression);
        _depth--;
    }

    private void PrintBinaryExpressionNode(BinaryExpressionNode node)
    {
        AppendLine($"BinaryExpressionNode: {node.Operator}");
        _depth++;
        Visit(node.Left);
        Visit(node.Right);
        _depth--;
    }

    private void PrintColumnReferenceNode(ColumnReferenceNode node)
    {
        AppendLine($"ColumnReferenceNode: {(node.TableName != null ? $"{node.TableName}." : "")}{node.ColumnName}");
    }

    private void PrintLiteralNode(LiteralNode node)
    {
        AppendLine($"LiteralNode: {node.Value}");
    }

    private void PrintFunctionNode(FunctionNode node)
    {
        AppendLine($"FunctionNode: {node.FunctionName}");
        _depth++;
        foreach (var arg in node.Arguments)
        {
            Visit(arg);
        }
        _depth--;
    }

    private void PrintSubqueryNode(SubqueryNode node)
    {
        AppendLine("SubqueryNode");
        _depth++;
        Visit(node.Select);
        _depth--;
    }

    private void PrintUnionNode(UnionNode node)
    {
        AppendLine($"UnionNode: {(node.IsUnionAll ? "UNION ALL" : "UNION")}");
        _depth++;
        Visit(node.Left);
        Visit(node.Right);
        _depth--;
    }

    private void AppendLine(string line)
    {
        _sb.AppendLine(new string(' ', _depth * 2) + line);
    }
    
    public override string ToString()
    {
        return _sb.ToString();
    }

    public void PrintAndClear()
    {
        Console.WriteLine(_sb.ToString());
        this.Reset();
    }
}