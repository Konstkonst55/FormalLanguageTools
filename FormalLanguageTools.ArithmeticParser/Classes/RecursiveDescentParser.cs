using FormalLanguageTools.ArithmeticParser.DataClasses;
using FormalLanguageTools.ArithmeticParser.Enums;
using FormalLanguageTools.ArithmeticParser.Exceptions;
using FormalLanguageTools.Shared.DataClasses;

namespace FormalLanguageTools.ArithmeticParser.Classes;

internal sealed class RecursiveDescentParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public RecursiveDescentParser(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count == 0 || tokens[^1].Type != TokenType.EndOfInput)
        {
            throw new ArgumentException("Последним токеном должен быть EndOfInput.", nameof(tokens));
        }

        _tokens = tokens;
    }

    private Token Current => _tokens[_position];

    public void Parse()
    {
        ParseExpression();
        Match(TokenType.EndOfInput);
    }

    private void ParseExpression()
    {
        ParseTerm();
        ParseExpressionPrime();
    }

    private void ParseExpressionPrime()
    {
        while (Current.Type is TokenType.Plus or TokenType.Minus)
        {
            Advance();
            ParseTerm();
        }
    }

    private void ParseTerm()
    {
        ParseFactor();
        ParseTermPrime();
    }

    private void ParseTermPrime()
    {
        while (Current.Type is TokenType.Multiply or TokenType.Divide)
        {
            Advance();
            ParseFactor();
        }
    }

    private void ParseFactor()
    {
        if (Current.Type is TokenType.Number or TokenType.Identifier)
        {
            Advance();
            return;
        }

        if (Current.Type == TokenType.LeftParenthesis)
        {
            Advance();
            ParseExpression();
            Match(TokenType.RightParenthesis);
            return;
        }

        throw CreateSyntaxException("number", "id", "'('");
    }

    private void Match(TokenType expectedType)
    {
        if (Current.Type != expectedType)
        {
            throw CreateSyntaxException(DescribeTokenType(expectedType));
        }

        if (expectedType != TokenType.EndOfInput)
        {
            Advance();
        }
    }

    private void Advance()
    {
        if (Current.Type != TokenType.EndOfInput)
        {
            _position++;
        }
    }

    private SyntaxException CreateSyntaxException(params string[] expectedTokens)
    {
        return new SyntaxException(Current.Position, expectedTokens, DescribeCurrentToken());
    }

    private string DescribeCurrentToken()
    {
        return Current.Type == TokenType.EndOfInput ? "конец ввода" : $"'{Current.Value}'";
    }

    private static string DescribeTokenType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Number => "number",
            TokenType.Identifier => "id",
            TokenType.Plus => "'+'",
            TokenType.Minus => "'-'",
            TokenType.Multiply => "'*'",
            TokenType.Divide => "'/'",
            TokenType.LeftParenthesis => "'('",
            TokenType.RightParenthesis => "')'",
            TokenType.EndOfInput => "конец выражения",
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }
}
