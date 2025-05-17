namespace SqlParser;

public class ExpressionBalancer
{
    private readonly BalanceContext _context;
    private readonly Dictionary<string, int> _precedence;

    public ExpressionBalancer(BalanceContext context = BalanceContext.Default)
    {
        _context = context;
        _precedence = new Dictionary<string, int>
        {
            { "~", 1 },
            { "*", 2 },
            { "/", 2 },
            { "%", 2 },
            { "+", 3 },
            { "-", 3 },
            { "&", 3 },
            { "^", 3 },
            { "|", 3 },
            { ">", 4 },
            { "<", 4 },
            { ">=", 4 },
            { "<=", 4 },
            { "<>", 4 },
            { "!=", 4 },
            { "!>", 4 },
            { "!<", 4 },
            { "NOT", 5 },
            { "AND", 6 },
            { "ALL", 7 },
            { "ANY", 7 },
            { "BETWEEN", 7 },
            { "IN", 7 },
            { "LIKE", 7 },
            { "OR", 7 },
            { "SOME", 7 }
        };

        // Add '=' with context-dependent precedence
        SetEqualsPrecedence();
    }

    private void SetEqualsPrecedence()
    {
        switch (_context)
        {
            case BalanceContext.Join:
            case BalanceContext.Where:
                _precedence["="] = 4;
                break;
            case BalanceContext.Default:
                _precedence["="] = 8;
                break;
        }
    }
    
    public ExpressionNode Balance(ExpressionNode expr)
    {
        if (expr is not BinaryExpressionNode binExpr)
            return expr;

        // First, balance the children
        var left = Balance(binExpr.Left);
        var right = Balance(binExpr.Right);

        binExpr = binExpr with { Left = left, Right = right };

        // Check if we need to rotate
        while (true)
        {
            if (binExpr.Left is BinaryExpressionNode leftChild && 
                GetPrecedence(leftChild) > GetPrecedence(binExpr))
            {
                // Rotate right
                binExpr = (BinaryExpressionNode)RotateRight(binExpr);
            }
            else if (binExpr.Right is BinaryExpressionNode rightChild && 
                     GetPrecedence(rightChild) > GetPrecedence(binExpr))
            {
                // Rotate left
                binExpr = (BinaryExpressionNode)RotateLeft(binExpr);
            }
            else
            {
                break;
            }
        }

        return binExpr;
    }

    private ExpressionNode RotateLeft(BinaryExpressionNode node)
    {
        var right = (BinaryExpressionNode)node.Right;
        return right with
        {
            Left = node with { Right = right.Left }
        };
    }

    private ExpressionNode RotateRight(BinaryExpressionNode node)
    {
        var left = (BinaryExpressionNode)node.Left;
        return left with
        {
            Right = node with { Left = left.Right }
        };
    }

    private int GetPrecedence(ExpressionNode expr)
    {
        if (expr is BinaryExpressionNode binExpr)
        {
            return _precedence.TryGetValue(binExpr.Operator, out int value) ? value : 0;
        }
        return int.MaxValue;  // Treat non-binary expressions as highest precedence
    }
}

public static class NodeExtensions
{
    public static WhereNode Balance(this WhereNode whereNode)
    {
        var balancer = new ExpressionBalancer(BalanceContext.Where);
        return whereNode with { Condition = balancer.Balance(whereNode.Condition) };
    }
    
    public static JoinNode Balance(this JoinNode joinNode)
    {
        if (joinNode.Condition == null) return joinNode;
        var balancer = new ExpressionBalancer(BalanceContext.Join);
        return joinNode with { Condition = balancer.Balance(joinNode.Condition) };
    }
}

public enum BalanceContext
{
    Default,
    Where,
    Join,
}