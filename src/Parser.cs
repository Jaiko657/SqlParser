using System.Security.AccessControl;
using Microsoft.VisualBasic.CompilerServices;

namespace SqlParser;

public class Parser
{
    private readonly List<Token> _tokens = [];
    private int _position;
    private Token Current => _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.Eof, "", -1, -1);

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }
    public Parser(Lexer lexer)
    {
        Token token;
        do
        {
            token = lexer.GetNextToken();
            _tokens.Add(token);
        } while (token.Type != TokenType.Eof);
        _position = 0;
    }

    private Token Peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _tokens.Count ? _tokens[index] : new Token(TokenType.Eof, "", -1, -1);
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Current;
    }
    
    private bool Match(TokenType type, params string[] lexeme)
    {
        if (Current.Type != type) return false;
        return lexeme.Length == 0 ||  lexeme.Contains(Current.Lexeme);
    }

    private void Expect(TokenType type, params string[] lexemes)
    {
        if (Current.Type != type)
        {
            throw new Exception($"Expected {type}, but found {Current.Type}");
        }

        if (lexemes.Length > 0 && !lexemes.Contains(Current.Lexeme))
        {
            throw new Exception($"Expected one of [{string.Join(", ", lexemes)}], but found '{Current.Lexeme}'");
        }
    }

    public Node Parse()
    {
        return ParseStatement();
    }

    private Node ParseStatement()
    {
        if (Match(TokenType.Keyword, "SELECT"))
        {
            return ParseSelectStatement();
        }
        throw new Exception($"Unexpected token: {Current}");
    }

    private SelectNode ParseSelectStatement()
    {
        Advance(); //Skip SELECT
        var columns = ParseColumns();
        Expect(TokenType.Keyword, "FROM");
        Advance();
        var fromTable = ParseTable();
        
        if(IsAtEnd()) 
            return new SelectNode(
                Columns: columns,
                FromTable: fromTable,
                Joins: []);
        
        if (!Match(TokenType.Keyword))
        {
            throw new Exception($"Expected Keyword, but found {Current}");    
        }

        var joins = ParseJoins();
        WhereNode? where = null;
        GroupByNode? groupBy = null;
        HavingNode? having = null;
        OrderByNode? orderBy = null;
        LimitNode? limit = null;
        
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
                   "RIGHT OUTER JOIN"))
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
            if(!Match(TokenType.Keyword, "ON") && Match(TokenType.Identifier))
            {
                var alias = new AliasNode(Current.Lexeme);
                table = table with { Alias = alias };
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
        
        if(!Match(TokenType.Identifier))
        {
            throw new Exception($"Expected identifier or '*', but found {Current}");
        }
    
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
        return new TableNode(
            TableName: tableName,
            Alias: alias);
    }
    
    private ExpressionNode ParseJoinExpression()
    {
        var left = ParseJoinExpressionTerm();
        
        while (Match(TokenType.Keyword, "AND", "OR"))
        {
            var op = Current.Lexeme;
            Advance();
            var right = ParseJoinExpressionTerm();
            left = new BinaryExpressionNode(left, op, right);
        }
        
        return left;
    }

    private ExpressionNode ParseJoinExpressionTerm()
    {
        if (Match(TokenType.Punctuation, "("))
        {
            Advance(); // Consume '('
            var expr = new GroupedExpressionNode(ParseJoinExpression()); 
            Expect(TokenType.Punctuation, ")");
            Advance(); // Consume ')'
            return expr;
        }
        
        var leftColumn = ParseColumnReference();
        
        var op = Current.Lexeme;
        Advance(); // Consume op

        ExpressionNode right;
        if (IsValue(Current))
        {
            right = ParseLiteral();
        }
        else
        {
            right = ParseColumnReference();
        }
        
        return new BinaryExpressionNode(leftColumn, op, right);
    }

    private ColumnReferenceNode ParseColumnReference()
    {
        if(!Match(TokenType.Identifier))
        {
            throw new Exception($"Expected identifier, but found {Current}");
        }
    
        var identifier = Current;
        Advance();
        if (Match(TokenType.Punctuation, "."))
        {
            Advance();
            Expect(TokenType.Identifier);
            var columnName = Current.Lexeme;
            Advance();
            return new ColumnReferenceNode(ColumnName: columnName, TableName: identifier.Lexeme);
        }
        return new ColumnReferenceNode(ColumnName: identifier.Lexeme);
    }

    private LiteralNode ParseLiteral()
    {
        switch (Current.Type)
        {
            case TokenType.String:
                var str = Current.Lexeme;
                Advance();
                return new LiteralNode(new String(str));
            case TokenType.Number:
                var num = Current.Lexeme;
                Advance();
                Double.TryParse(num, out var number);
                return new LiteralNode(number);
            default:
                throw new Exception($"Couldn't Parse Token as Literal Node: {Current}");
        }
    }

    private static bool IsValue(Token token)
    {
        return token.Type is TokenType.String or TokenType.Number;
    }
    
    private bool IsAtEnd()
    {
        return _position >= _tokens.Count;
    }
    
    public void PrintTokens()
    {
        foreach (var token in _tokens)
        {
            Console.WriteLine(token);
        }
        Console.WriteLine();
    }
}