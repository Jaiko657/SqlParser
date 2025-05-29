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

    [TestMethod]
    public void TestSimpleWhere()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2 WHERE col = 1";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Where);
        Assert.IsInstanceOfType(selectNode.Where.Condition, typeof(BinaryExpressionNode));
        var cond = (BinaryExpressionNode)selectNode.Where.Condition;
        Assert.AreEqual("=", cond.Operator);
    }

    [TestMethod]
    public void TestWhereWithAndOrPrecedence()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a WHERE x = 1 OR y = 2 AND z = 3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Where);
        Assert.IsInstanceOfType(selectNode.Where.Condition, typeof(BinaryExpressionNode));
        var top = (BinaryExpressionNode)selectNode.Where.Condition;
        Assert.AreEqual("OR", top.Operator);
        Assert.IsInstanceOfType(top.Right, typeof(BinaryExpressionNode));
        Assert.AreEqual("AND", ((BinaryExpressionNode)top.Right).Operator);
    }

    [TestMethod]
    public void TestWhereWithParentheses()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a WHERE (x = 1 OR y = 2) AND z = 3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Where);
        Assert.IsInstanceOfType(selectNode.Where.Condition, typeof(BinaryExpressionNode));
        var top = (BinaryExpressionNode)selectNode.Where.Condition;
        Assert.AreEqual("AND", top.Operator);
        Assert.IsInstanceOfType(top.Left, typeof(GroupedExpressionNode));
    }

    [TestMethod]
    public void TestWhereWithStringLiteral()
    {
        string sql = "SELECT * FROM t JOIN u ON t.id = u.id WHERE name = 'foo'";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Where);
        Assert.IsInstanceOfType(selectNode.Where.Condition, typeof(BinaryExpressionNode));
        var cond = (BinaryExpressionNode)selectNode.Where.Condition;
        Assert.IsInstanceOfType(cond.Right, typeof(LiteralNode));
        Assert.AreEqual("foo", ((LiteralNode)cond.Right).Value);
    }

    [TestMethod]
    public void TestNoWhereWhenOnlyJoins()
    {
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNull(selectNode.Where);
    }

    [TestMethod]
    public void TestOrderBySingleColumn()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY col";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(SelectNode));
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.OrderBy);
        Assert.AreEqual(1, selectNode.OrderBy.Items.Count);
        Assert.AreEqual("col", selectNode.OrderBy.Items[0].Column.ColumnName);
        Assert.AreEqual(SortDirection.Ascending, selectNode.OrderBy.Items[0].Direction);
    }

    [TestMethod]
    public void TestOrderByDesc()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY col DESC";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.OrderBy);
        Assert.AreEqual(1, selectNode.OrderBy.Items.Count);
        Assert.AreEqual(SortDirection.Descending, selectNode.OrderBy.Items[0].Direction);
    }

    [TestMethod]
    public void TestOrderByMultiple()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY a, b DESC, c ASC";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.OrderBy);
        Assert.AreEqual(3, selectNode.OrderBy.Items.Count);
        Assert.AreEqual(SortDirection.Ascending, selectNode.OrderBy.Items[0].Direction);
        Assert.AreEqual(SortDirection.Descending, selectNode.OrderBy.Items[1].Direction);
        Assert.AreEqual(SortDirection.Ascending, selectNode.OrderBy.Items[2].Direction);
    }

    [TestMethod]
    public void TestNoOrderBy()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a WHERE x = 1";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNull(selectNode.OrderBy);
    }

    [TestMethod]
    public void TestLimitOnly()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a LIMIT 10";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Limit);
        Assert.AreEqual(10, selectNode.Limit.Limit);
        Assert.IsNull(selectNode.Limit.Offset);
    }

    [TestMethod]
    public void TestLimitOffset()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY a LIMIT 5 OFFSET 20";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Limit);
        Assert.AreEqual(5, selectNode.Limit.Limit);
        Assert.AreEqual(20, selectNode.Limit.Offset);
    }

    [TestMethod]
    public void TestNoLimit()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY a";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNull(selectNode.Limit);
    }

    [TestMethod]
    public void TestGroupByOnly()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a GROUP BY col";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.GroupBy);
        Assert.AreEqual(1, selectNode.GroupBy.Columns.Count);
        Assert.AreEqual("col", selectNode.GroupBy.Columns[0].ColumnName);
        Assert.IsNull(selectNode.Having);
    }

    [TestMethod]
    public void TestGroupByMultipleColumns()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a GROUP BY a, b, c";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.GroupBy);
        Assert.AreEqual(3, selectNode.GroupBy.Columns.Count);
        Assert.AreEqual("a", selectNode.GroupBy.Columns[0].ColumnName);
        Assert.AreEqual("b", selectNode.GroupBy.Columns[1].ColumnName);
        Assert.AreEqual("c", selectNode.GroupBy.Columns[2].ColumnName);
    }

    [TestMethod]
    public void TestHavingOnly()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a HAVING cnt > 10";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNull(selectNode.GroupBy);
        Assert.IsNotNull(selectNode.Having);
        Assert.IsInstanceOfType(selectNode.Having.Condition, typeof(BinaryExpressionNode));
        var cond = (BinaryExpressionNode)selectNode.Having.Condition;
        Assert.AreEqual(">", cond.Operator);
    }

    [TestMethod]
    public void TestHavingWithAndOrPrecedence()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a HAVING x = 1 OR y = 2 AND z = 3";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Having);
        var top = (BinaryExpressionNode)selectNode.Having.Condition;
        Assert.AreEqual("OR", top.Operator);
        Assert.IsInstanceOfType(top.Right, typeof(BinaryExpressionNode));
        Assert.AreEqual("AND", ((BinaryExpressionNode)top.Right).Operator);
    }

    [TestMethod]
    public void TestGroupByAndHaving()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a WHERE x = 1 GROUP BY a, b HAVING sum > 0 ORDER BY a LIMIT 5";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNotNull(selectNode.Where);
        Assert.IsNotNull(selectNode.GroupBy);
        Assert.AreEqual(2, selectNode.GroupBy.Columns.Count);
        Assert.IsNotNull(selectNode.Having);
        Assert.IsInstanceOfType(selectNode.Having.Condition, typeof(BinaryExpressionNode));
        Assert.AreEqual(">", ((BinaryExpressionNode)selectNode.Having.Condition).Operator);
        Assert.IsNotNull(selectNode.OrderBy);
        Assert.IsNotNull(selectNode.Limit);
        Assert.AreEqual(5, selectNode.Limit.Limit);
    }

    [TestMethod]
    public void TestNoGroupByOrHaving()
    {
        string sql = "SELECT * FROM t JOIN u ON t.a = u.a ORDER BY a";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var selectNode = (SelectNode)sqlNode;
        Assert.IsNull(selectNode.GroupBy);
        Assert.IsNull(selectNode.Having);
    }

    [TestMethod]
    public void TestDeleteNoWhere()
    {
        string sql = "DELETE FROM t";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        Assert.IsInstanceOfType(sqlNode, typeof(DeleteNode));
        var deleteNode = (DeleteNode)sqlNode;
        Assert.AreEqual("t", deleteNode.Table.TableName);
        Assert.IsNull(deleteNode.Where);
    }

    [TestMethod]
    public void TestDeleteWithWhere()
    {
        string sql = "DELETE FROM t WHERE id = 1";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var deleteNode = (DeleteNode)sqlNode;
        Assert.IsNotNull(deleteNode.Where);
        Assert.IsInstanceOfType(deleteNode.Where.Condition, typeof(BinaryExpressionNode));
        Assert.AreEqual("=", ((BinaryExpressionNode)deleteNode.Where.Condition).Operator);
    }

    [TestMethod]
    public void TestDeleteWithTableAlias()
    {
        string sql = "DELETE FROM schema.tbl t WHERE t.id = 0";
        var tokens = new Lexer(sql).GetAllTokens();
        var parser = new Parser(tokens);
        var sqlNode = parser.Parse();
        var deleteNode = (DeleteNode)sqlNode;
        Assert.AreEqual("schema.tbl", deleteNode.Table.TableName);
        Assert.IsNotNull(deleteNode.Table.Alias);
        Assert.AreEqual("t", deleteNode.Table.Alias.AliasName);
    }
}
