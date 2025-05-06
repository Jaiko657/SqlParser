using System.Text;

namespace SqlParser;

public class Lexer
{
    private readonly string _input;
    private int _position;
    private char _currentChar;

    private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "AND", "OR", "INSERT", "UPDATE", "DELETE",
        "JOIN", "ON", "GROUP", "BY", "HAVING", "ORDER", "LIMIT", "OFFSET",
        "AS", "INTO", "VALUES", "SET", "LEFT", "RIGHT", "INNER", "OUTER", "FULL",
        "UNION", "ALL", "DISTINCT", "TOP", "PERCENT", "WITH"
    };

    private static readonly HashSet<string> Operators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "=", "<", ">", "<=", ">=", "<>", "!=",
        "LIKE", "IN", "BETWEEN", "IS", "NOT", "NULL",
        "+", "-", "*", "/", "%", "||"
    };

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
        return new Token(TokenType.Number, result.ToString(), start);
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
            return new Token(TokenType.Keyword, word, start);
        
        if (Operators.Contains(word, StringComparer.OrdinalIgnoreCase))
            return new Token(TokenType.Operator, word, start);

        return new Token(TokenType.Identifier, word, start);
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
        return new Token(TokenType.String, result.ToString(), start);
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
            return new Token(TokenType.Operator, op, start);

        throw new Exception($"Invalid operator: {op}");
    }

    private Token ReadPunctuation()
    {
        var punctuation = _currentChar.ToString();
        var start = _position;
        Advance();
        return new Token(TokenType.Punctuation, punctuation, start);
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

        return new Token(TokenType.Eof, "", _position);
    }
}