using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlParser;
using System.Text;

namespace SqlParserTests;

[TestClass]
public class ComprehensiveJoinTests
{
    [TestMethod]
    public void TestSimpleInnerJoin()
    {
        AssertJoinParseTree(
            "SELECT * FROM table1 JOIN table2 ON id1 = id2",
            "Inner",
            "="
        );
    }
    
    [TestMethod]
    public void TestLeftJoin()
    {
        AssertJoinParseTree(
            "SELECT * FROM table1 LEFT JOIN table2 ON id1 = id2",
            "Left",
            "="
        );
    }
    
    [TestMethod]
    public void TestRightJoin()
    {
        AssertJoinParseTree(
            "SELECT * FROM table1 RIGHT JOIN table2 ON id1 = id2",
            "Right",
            "="
        );
    }
    
    [TestMethod]
    public void TestJoinWithAndPrecedence()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2 AND id3 = id4 AND id5 = id6";
        
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        // Verify structure - should have a series of AND expressions
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Verify top level join condition is AND
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        Assert.AreEqual("AND", condition.Operator);
        
        // Left side of AND should be another AND expression
        Assert.IsInstanceOfType(condition.Left, typeof(BinaryExpressionNode));
        var leftCondition = (BinaryExpressionNode)condition.Left;
        Assert.AreEqual("AND", leftCondition.Operator);
    }
    
    [TestMethod]
    public void TestJoinWithOrPrecedence()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2 OR id3 = id4 OR id5 = id6";
        
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        // Verify structure - should have a series of OR expressions
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Verify top level join condition is OR
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        Assert.AreEqual("OR", condition.Operator);
        
        // Left side of OR should be another OR expression
        Assert.IsInstanceOfType(condition.Left, typeof(BinaryExpressionNode));
        var leftCondition = (BinaryExpressionNode)condition.Left;
        Assert.AreEqual("OR", leftCondition.Operator);
    }
    
    [TestMethod]
    public void TestJoinWithMixedAndOrPrecedence()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON " +
                     "id1 = id2 AND id3 = id4 OR id5 = id6 AND id7 = id8";
        
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        // Get tree representation for visual inspection
        var printer = new TreePrinter();
        sqlNode.Accept(printer);
        string treeOutput = printer.ToString();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        // Verify structure - should follow operator precedence (AND before OR)
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Top level should be OR
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        Assert.AreEqual("OR", condition.Operator);
        
        // Left side of OR should be AND
        Assert.IsInstanceOfType(condition.Left, typeof(BinaryExpressionNode));
        var leftCondition = (BinaryExpressionNode)condition.Left;
        Assert.AreEqual("AND", leftCondition.Operator);
        
        // Right side of OR should be AND
        Assert.IsInstanceOfType(condition.Right, typeof(BinaryExpressionNode));
        var rightCondition = (BinaryExpressionNode)condition.Right;
        Assert.AreEqual("AND", rightCondition.Operator);
    }
    
    [TestMethod]
    public void TestJoinWithNestedParentheses()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON " +
                     "(id1 = id2 OR (id3 = id4 AND id5 = id6))";
        
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Top level should be grouped expression
        Assert.IsInstanceOfType(joinNode.Condition, typeof(GroupedExpressionNode));
        var groupedExpr = (GroupedExpressionNode)joinNode.Condition;
        
        // Inside group should be OR
        Assert.IsInstanceOfType(groupedExpr.InnerExpression, typeof(BinaryExpressionNode));
        var orExpr = (BinaryExpressionNode)groupedExpr.InnerExpression;
        Assert.AreEqual("OR", orExpr.Operator);
        
        // Right side of OR should be another grouped expression
        Assert.IsInstanceOfType(orExpr.Right, typeof(GroupedExpressionNode));
        var nestedGroupedExpr = (GroupedExpressionNode)orExpr.Right;
        
        // Inside nested group should be AND
        Assert.IsInstanceOfType(nestedGroupedExpr.InnerExpression, typeof(BinaryExpressionNode));
        var andExpr = (BinaryExpressionNode)nestedGroupedExpr.InnerExpression;
        Assert.AreEqual("AND", andExpr.Operator);
    }
    
    [TestMethod]
    [DataRow("a = b OR c = d AND e = f", "OR")]
    [DataRow("a = b AND c = d OR e = f", "OR")]
    [DataRow("(a = b OR c = d) AND e = f", "AND")]
    [DataRow("a = b AND (c = d OR e = f)", "AND")]
    public void TestJoinConditionPrecedence(string condition, string expectedTopOperator)
    {
        string sql = $"SELECT * FROM table1 JOIN table2 ON {condition}";
        
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        if (condition.Contains("("))
        {
            // If there are parentheses, the condition might be a grouped expression
            if (joinNode.Condition is GroupedExpressionNode groupedExpr)
            {
                // Extract inner expression
                Assert.IsInstanceOfType(groupedExpr.InnerExpression, typeof(BinaryExpressionNode));
                var binaryExpr = (BinaryExpressionNode)groupedExpr.InnerExpression;
                Assert.AreEqual(expectedTopOperator, binaryExpr.Operator);
            }
            else
            {
                Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
                var binaryExpr = (BinaryExpressionNode)joinNode.Condition;
                Assert.AreEqual(expectedTopOperator, binaryExpr.Operator);
            }
        }
        else
        {
            // No parentheses, should be a binary expression
            Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
            var binaryExpr = (BinaryExpressionNode)joinNode.Condition;
            Assert.AreEqual(expectedTopOperator, binaryExpr.Operator);
        }
    }
    
    // Helper method to assert join parse tree
    private void AssertJoinParseTree(string sql, string expectedJoinType, string expectedConditionOperator)
    {
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Check join type
        Assert.AreEqual(Enum.Parse<JoinType>(expectedJoinType), joinNode.Type);
        
        // Check condition operator
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        Assert.AreEqual(expectedConditionOperator, condition.Operator);
    }
}
