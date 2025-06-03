using System.Diagnostics;
using System.Text;

namespace SqlParser;

public class Lexer
{
    private readonly string _input;
    private int _position;
    private char _currentChar;

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "INSERT", "UPDATE", "DELETE",
        "JOIN", "ON", "GROUP", "BY", "HAVING", "ORDER", "LIMIT", "OFFSET",
        "AS", "INTO", "VALUES", "SET", "LEFT", "RIGHT", "INNER", "OUTER", "FULL", "CROSS",
        "UNION", "ALL", "DISTINCT", "TOP", "PERCENT", "WITH",
        "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", "LEFT OUTER JOIN", "RIGHT OUTER JOIN", "CROSS JOIN"
    }; //TODO: There are more keywords

    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "=", "<", ">", "<=", ">=", "<>", "!=",
        "LIKE", "IN", "BETWEEN", "IS", "NOT", "NULL",
        "+", "-", "*", "/", "%", "||"
    }; //TODO: There are more operators, or some may be better treated as keywords

    public Lexer(string input)
    {
        _input = input;
        _position = 0;
        _currentChar = _input.Length > 0 ? _input[0] : '\0';
    }

    private void Advance()
    {
        _position++;
        if (_position < _input.Length)
            _currentChar = _input[_position];
        else
            _currentChar = '\0';
    }

    private void SkipWhitespace()
    {
        while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar))
            Advance();
    }

    private Token CreateToken(TokenType type, string value, int startPosition)
    {
        Debug.Assert(startPosition >= 0);
        if (type == TokenType.Eof) return new Token(type, value, startPosition, startPosition);
        if (type == TokenType.Keyword) return new Token(type, value.ToUpper(), startPosition, startPosition + value.Length - 1);
        return new Token(type, value, startPosition, startPosition + value.Length - 1);
    }

    private Token ReadNumber()
    {
        var result = new StringBuilder();
        var start = _position;
        bool hasDecimalPoint = false;

        while (_currentChar != '\0' && (char.IsDigit(_currentChar) || _currentChar == '.'))
        {
            if (_currentChar == '.')
            {
                if (hasDecimalPoint)
                    throw new Exception("Invalid number format: multiple decimal points.");
                hasDecimalPoint = true;
            }
            result.Append(_currentChar);
            Advance();
        }
        return CreateToken(TokenType.Number, result.ToString(), start);
    }

    private Token ReadIdentifierOrKeyword()
    {
        var result = new StringBuilder();
        var start = _position;

        while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
        {
            result.Append(_currentChar);
            Advance();
        }

        string word = result.ToString();

        if (Keywords.Contains(word, StringComparer.OrdinalIgnoreCase))
        {
            Token multiWordKeyword = TryReadMultiWordKeyword(word, start);
            return multiWordKeyword ?? CreateToken(TokenType.Keyword, word, start);
        }
        
        if (Operators.Contains(word, StringComparer.OrdinalIgnoreCase))
            return CreateToken(TokenType.Operator, word, start);

        return CreateToken(TokenType.Identifier, word, start);
    }

    private Token TryReadMultiWordKeyword(string firstWord, int start)
    {
        int savedPosition = _position;
        char savedChar = _currentChar;

        SkipWhitespace();
        Token nextWord = ReadIdentifierOrKeyword();

        if (nextWord.Type == TokenType.Keyword)
        {
            string combinedKeyword = $"{firstWord} {nextWord.Lexeme}".ToUpper();;
            if (Keywords.Contains(combinedKeyword, StringComparer.OrdinalIgnoreCase))
                return CreateToken(TokenType.Keyword, combinedKeyword, start);
        }

        // Restore position if it's not a multi-word keyword
        _position = savedPosition;
        _currentChar = savedChar;
        return null;
    }

    private Token ReadString()
    {
        char quoteChar = _currentChar;
        var result = new StringBuilder();
        var start = _position;
        Advance();
        while (_currentChar != '\0' && _currentChar != quoteChar)
        {
            if (_currentChar == '\\')
            {
                Advance();
                if (_currentChar == '\0')
                    throw new Exception("Unterminated string literal");
            }
            result.Append(_currentChar);
            Advance();
        }
        if (_currentChar == quoteChar)
            Advance();
        else
            throw new Exception("Unterminated string literal");
        return CreateToken(TokenType.String, result.ToString(), start);
    }

    private Token ReadOperator()
    {
        var result = new StringBuilder();
        var start = _position;

        while (_currentChar != '\0' && (char.IsLetter(_currentChar) || "=<>!+-*/%".IndexOf(_currentChar) != -1))
        {
            result.Append(_currentChar);
            Advance();
        }

        string op = result.ToString();

        if (Operators.Contains(op, StringComparer.OrdinalIgnoreCase))
            return CreateToken(TokenType.Operator, op, start);

        throw new Exception($"Invalid operator: {op}");
    }

    private Token ReadPunctuation()
    {
        var punctuation = _currentChar.ToString();
        var start = _position;
        Advance();
        return CreateToken(TokenType.Punctuation, punctuation, start);
    }

    public Token GetNextToken()
    {
        while (_currentChar != '\0')
        {
            if (char.IsWhiteSpace(_currentChar))
            {
                SkipWhitespace();
                continue;
            }

            if (char.IsLetter(_currentChar) || _currentChar == '_')
                return ReadIdentifierOrKeyword();

            if (char.IsDigit(_currentChar))
                return ReadNumber();

            if (_currentChar == '"' || _currentChar == '\'')
                return ReadString();

            if (_currentChar == '=' || _currentChar == '<' || _currentChar == '>' || _currentChar == '!')
                return ReadOperator();

            switch (_currentChar)
            {
                case '.':
                case ',':
                case ';':
                case '(':
                case ')':
                    return ReadPunctuation();
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '|':
                    return ReadOperator();
                default:
                    throw new Exception($"Invalid character: {_currentChar}");
            }
        }

        return CreateToken(TokenType.Eof, "", _position);
    }

    public List<Token> GetAllTokens()
    {
        if(_position != 0)
            throw new Exception("Lexer has already been used. Create a new instance.");
        var tokens = new List<Token>();
        Token token;
        do
        {
            token = GetNextToken();
            tokens.Add(token);
        } while (token.Type != TokenType.Eof);
        return tokens;
    }
}