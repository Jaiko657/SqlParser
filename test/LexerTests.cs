using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlParser;

namespace SqlParserTests;

[TestClass]
public class LexerTests
{
    [TestMethod]
    public void TestBasicSelectStatement()
    {
        // Arrange
        string sql = "SELECT * FROM table";
        
        // Act
        var tokens = new Lexer(sql).GetAllTokens();
        
        // Assert
        Assert.AreEqual(5, tokens.Count); // SELECT, *, FROM, table, EOF
        
        Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
        Assert.AreEqual("SELECT", tokens[0].Lexeme);
        
        Assert.AreEqual(TokenType.Operator, tokens[1].Type);
        Assert.AreEqual("*", tokens[1].Lexeme);
        
        Assert.AreEqual(TokenType.Keyword, tokens[2].Type);
        Assert.AreEqual("FROM", tokens[2].Lexeme);
        
        Assert.AreEqual(TokenType.Identifier, tokens[3].Type);
        Assert.AreEqual("table", tokens[3].Lexeme);
        
        Assert.AreEqual(TokenType.Eof, tokens[4].Type);
    }
    
    [TestMethod]
    public void TestJoinStatement()
    {
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2";
        
        // Act
        var tokens = new Lexer(sql).GetAllTokens();
        
        // Assert
        Assert.AreEqual(11, tokens.Count); // SELECT, *, FROM, table1, JOIN, table2, ON, id1, =, id2, EOF
        
        Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
        Assert.AreEqual("SELECT", tokens[0].Lexeme);
        
        Assert.AreEqual(TokenType.Keyword, tokens[4].Type);
        Assert.AreEqual("JOIN", tokens[4].Lexeme);
        
        Assert.AreEqual(TokenType.Keyword, tokens[6].Type);
        Assert.AreEqual("ON", tokens[6].Lexeme);
        
        Assert.AreEqual(TokenType.Operator, tokens[8].Type);
        Assert.AreEqual("=", tokens[8].Lexeme);
    }
    
    [TestMethod]
    public void TestJoinWithAndOr()
    {
        // Arrange
        string sql = "SELECT * FROM table1 JOIN table2 ON id1 = id2 AND id3 = id4 OR id5 = id6";
        
        // Act
        var tokens = new Lexer(sql).GetAllTokens();
        
        // Assert
        Assert.AreEqual(19, tokens.Count); 
        
        // Check for correct tokenization of AND and OR operators
        Assert.AreEqual(TokenType.Keyword, tokens[10].Type);
        Assert.AreEqual("AND", tokens[10].Lexeme);
        
        Assert.AreEqual(TokenType.Keyword, tokens[14].Type);
        Assert.AreEqual("OR", tokens[14].Lexeme);
    }
}
