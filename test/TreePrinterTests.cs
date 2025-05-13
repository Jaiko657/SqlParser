using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlParser;
using System.Text;

namespace SqlParserTests;

[TestClass]
public class TreePrinterTests
{
    [TestMethod]
    public void TestPrintTreeStructureForJoinWithAndOr()
    {
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id1 OR id2 = id2 AND id3 = id3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        // Act
        var printer = new TreePrinter();
        sqlNode.Accept(printer);
        string output = printer.ToString();
        
        // Assert
        Assert.IsTrue(output.Contains("SelectNode"));
        Assert.IsTrue(output.Contains("JoinNode: Inner"));
        Assert.IsTrue(output.Contains("BinaryExpressionNode: OR"));
        Assert.IsTrue(output.Contains("BinaryExpressionNode: AND"));
        
        // Verify the structure - OR should be the top level condition for the join
        int orIndex = output.IndexOf("BinaryExpressionNode: OR");
        int andIndex = output.IndexOf("BinaryExpressionNode: AND");
        
        // AND should appear after OR in the tree (as a child)
        Assert.IsTrue(orIndex < andIndex);
    }
    
    [TestMethod]
    public void TestPrintTreeStructureForParenthesizedExpression()
    {
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON (id1 = id1 OR id2 = id2) AND id3 = id3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        
        // Act
        var printer = new TreePrinter();
        sqlNode.Accept(printer);
        string output = printer.ToString();
        
        // Assert
        Assert.IsTrue(output.Contains("GroupedExpressionNode"));
        Assert.IsTrue(output.Contains("BinaryExpressionNode: AND"));
        Assert.IsTrue(output.Contains("BinaryExpressionNode: OR"));
        
        // AND should be the top-level operator due to parentheses changing precedence
        int andIndex = output.IndexOf("BinaryExpressionNode: AND");
        int groupedIndex = output.IndexOf("GroupedExpressionNode");
        
        // GroupedExpression should appear after AND in the tree (as a child)
        Assert.IsTrue(andIndex < groupedIndex);
    }
}
