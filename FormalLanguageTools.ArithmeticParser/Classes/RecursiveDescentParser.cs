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

    public SyntaxNode Parse()
    {
        var root = ParseExpression();
        Match(TokenType.EndOfInput);
        return root;
    }

    private SyntaxNode ParseExpression()
    {
        var term = ParseTerm();
        var expressionPrime = ParseExpressionPrime();
        return new SyntaxNode("E", children: new[] { term, expressionPrime });
    }

    private SyntaxNode ParseExpressionPrime()
    {
        var suffixes = new List<(SyntaxNode Operator, SyntaxNode Term)>();
        while (Current.Type is TokenType.Plus or TokenType.Minus)
        {
            var operation = ConsumeTerminal(Current.Type);
            var term = ParseTerm();
            suffixes.Add((operation, term));
        }

        SyntaxNode result = new("E'", children: new[] { new SyntaxNode("ε") });
        for (var index = suffixes.Count - 1; index >= 0; index--)
        {
            result = new SyntaxNode("E'", children: new[] { suffixes[index].Operator, suffixes[index].Term, result });
        }

        return result;
    }

    private SyntaxNode ParseTerm()
    {
        var factor = ParseFactor();
        var termPrime = ParseTermPrime();
        return new SyntaxNode("T", children: new[] { factor, termPrime });
    }

    private SyntaxNode ParseTermPrime()
    {
        var suffixes = new List<(SyntaxNode Operator, SyntaxNode Factor)>();
        while (Current.Type is TokenType.Multiply or TokenType.Divide)
        {
            var operation = ConsumeTerminal(Current.Type);
            var factor = ParseFactor();
            suffixes.Add((operation, factor));
        }

        SyntaxNode result = new("T'", children: new[] { new SyntaxNode("ε") });
        for (var index = suffixes.Count - 1; index >= 0; index--)
        {
            result = new SyntaxNode("T'", children: new[] { suffixes[index].Operator, suffixes[index].Factor, result });
        }

        return result;
    }

    private SyntaxNode ParseFactor()
    {
        if (Current.Type is TokenType.Number or TokenType.Identifier)
        {
            var tokenType = Current.Type;
            var terminal = ConsumeTerminal(tokenType);
            return new SyntaxNode("F", children: new[] { terminal });
        }

        if (Current.Type == TokenType.LeftParenthesis)
        {
            var leftParenthesis = ConsumeTerminal(TokenType.LeftParenthesis);
            var expression = ParseExpression();
            var rightParenthesis = ConsumeTerminal(TokenType.RightParenthesis);
            return new SyntaxNode("F", children: new[] { leftParenthesis, expression, rightParenthesis });
        }

        throw CreateSyntaxException("number", "id", "'('");
    }

    private SyntaxNode ConsumeTerminal(TokenType expectedType)
    {
        if (Current.Type != expectedType)
        {
            throw CreateSyntaxException(DescribeTokenType(expectedType));
        }

        var token = Current;
        Advance();
        var name = DescribeTreeTerminal(token.Type);
        var value = token.Type is TokenType.Number or TokenType.Identifier ? token.Value : null;
        return new SyntaxNode(name, value);
    }

    private void Match(TokenType expectedType)
    {
        if (Current.Type != expectedType)
        {
            throw CreateSyntaxException(DescribeTokenType(expectedType));
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

    private static string DescribeTreeTerminal(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Number => "number",
            TokenType.Identifier => "id",
            TokenType.Plus => "+",
            TokenType.Minus => "-",
            TokenType.Multiply => "*",
            TokenType.Divide => "/",
            TokenType.LeftParenthesis => "(",
            TokenType.RightParenthesis => ")",
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }
}
