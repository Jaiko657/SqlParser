using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlParser;
using System.Text;

namespace SqlParserTests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    public void TestJoinWithAndPrecedenceOverOr()
    {
        // Test that AND has higher precedence than OR in join conditions
        
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id1 OR id2 = id2 AND id3 = id3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        
        // Assert
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        // Verify joins
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Verify join condition
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        
        // The top operator should be OR since AND has higher precedence
        Assert.AreEqual("OR", condition.Operator);
        
        // Left side should be a simple equality
        Assert.IsInstanceOfType(condition.Left, typeof(BinaryExpressionNode));
        var leftCondition = (BinaryExpressionNode)condition.Left;
        Assert.AreEqual("=", leftCondition.Operator);
        
        // Right side should be AND expression
        Assert.IsInstanceOfType(condition.Right, typeof(BinaryExpressionNode));
        var rightCondition = (BinaryExpressionNode)condition.Right;
        Assert.AreEqual("AND", rightCondition.Operator);
    }
    
    [TestMethod]
    public void TestComplexJoinWithMultipleOperators()
    {
        // Test complex join with multiple operators to verify precedence
        
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id1 OR id2 = id2 AND id3 = id3 OR id4 = id4";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        
        // Assert
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // Verify the parsed condition tree structure
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        
        // The top operator should be OR
        Assert.AreEqual("OR", condition.Operator);
        
        // Right side should be a simple equality for id4 = id4
        Assert.IsInstanceOfType(condition.Right, typeof(BinaryExpressionNode));
        var rightCondition = (BinaryExpressionNode)condition.Right;
        Assert.AreEqual("=", rightCondition.Operator);
        
        // Left side should be another OR expression
        Assert.IsInstanceOfType(condition.Left, typeof(BinaryExpressionNode));
        var leftCondition = (BinaryExpressionNode)condition.Left;
        Assert.AreEqual("OR", leftCondition.Operator);
        
        // Left side of the left condition should be a simple equality
        Assert.IsInstanceOfType(leftCondition.Left, typeof(BinaryExpressionNode));
        
        // Right side of the left condition should be an AND expression
        Assert.IsInstanceOfType(leftCondition.Right, typeof(BinaryExpressionNode));
        var nestedRightCondition = (BinaryExpressionNode)leftCondition.Right;
        Assert.AreEqual("AND", nestedRightCondition.Operator);
    }
    
    [TestMethod]
    public void TestJoinWithParenthesizedExpression()
    {
        // Test that parentheses change the precedence
        
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON (id1 = id1 OR id2 = id2) AND id3 = id3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        
        // Assert
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        Assert.AreEqual(1, selectNode.Joins.Count);
        var joinNode = selectNode.Joins[0];
        
        // The top operator should be AND since parentheses changed precedence
        Assert.IsInstanceOfType(joinNode.Condition, typeof(BinaryExpressionNode));
        var condition = (BinaryExpressionNode)joinNode.Condition;
        Assert.AreEqual("AND", condition.Operator);
        
        // Left side should be a grouped expression containing OR
        Assert.IsInstanceOfType(condition.Left, typeof(GroupedExpressionNode));
        var groupedExpr = (GroupedExpressionNode)condition.Left;
        Assert.IsInstanceOfType(groupedExpr.InnerExpression, typeof(BinaryExpressionNode));
        var innerCondition = (BinaryExpressionNode)groupedExpr.InnerExpression;
        Assert.AreEqual("OR", innerCondition.Operator);
    }
    
    [TestMethod]
    public void TestMultipleJoins()
    {
        // Test multiple joins to ensure they're all parsed correctly
        
        // Arrange
        string sql = "SELECT * FROM table1 " +
                     "JOIN table2 ON id1 = id2 " +
                     "JOIN table3 ON id3 = id4 AND id5 = id6";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        
        // Assert
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        
        // Should have two joins
        Assert.AreEqual(2, selectNode.Joins.Count);
        
        // First join should have simple condition
        var join1 = selectNode.Joins[0];
        Assert.IsInstanceOfType(join1.Condition, typeof(BinaryExpressionNode));
        var condition1 = (BinaryExpressionNode)join1.Condition;
        Assert.AreEqual("=", condition1.Operator);
        
        // Second join should have AND condition
        var join2 = selectNode.Joins[1];
        Assert.IsInstanceOfType(join2.Condition, typeof(BinaryExpressionNode));
        var condition2 = (BinaryExpressionNode)join2.Condition;
        Assert.AreEqual("AND", condition2.Operator);
    }
    
    [TestMethod]
    public void TestPrintTree()
    {
        // Test printing the tree to visualize the structure
        
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id1 OR id2 = id2 AND id3 = id3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        var printer = new TreePrinter();
        sqlNode.Accept(printer);
        string treeOutput = printer.ToString();
        
        // Assert
        Assert.IsTrue(treeOutput.Contains("BinaryExpressionNode: OR"));
        Assert.IsTrue(treeOutput.Contains("BinaryExpressionNode: AND"));
        
        // Print for inspection (uncomment during debugging)
        // Console.WriteLine(treeOutput);
    }
    
    [TestMethod]
    [DataRow("SELECT * FROM table1 JOIN table2 ON id1 = id1 OR id2 = id2 AND id3 = id3")]
    [DataRow("SELECT * FROM table1 JOIN table2 ON id1 = id1 AND id2 = id2 OR id3 = id3")]
    [DataRow("SELECT * FROM table1 JOIN table2 ON id1 = id1 AND (id2 = id2 OR id3 = id3)")]
    [DataRow("SELECT * FROM table1 JOIN table2 ON (id1 = id1 OR id2 = id2) AND id3 = id3")]
    public void TestVariousJoinExpressions(string sql)
    {
        // Test different join expressions
        
        // Arrange
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        
        // Act
        var sqlNode = parser.Parse();
        var printer = new TreePrinter();
        sqlNode.Accept(printer);
        string treeOutput = printer.ToString();
        
        // Assert - just verify that parsing succeeded
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        
        // Print for inspection (uncomment during debugging)
        // Console.WriteLine($"SQL: {sql}");
        // Console.WriteLine(treeOutput);
        // Console.WriteLine("---------------------------");
    }
}
