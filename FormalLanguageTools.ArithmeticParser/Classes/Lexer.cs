using FormalLanguageTools.ArithmeticParser.DataClasses;
using FormalLanguageTools.ArithmeticParser.Enums;
using FormalLanguageTools.ArithmeticParser.Exceptions;
using FormalLanguageTools.Shared.DataClasses;

namespace FormalLanguageTools.ArithmeticParser.Classes;

internal static class Lexer
{
    public static IReadOnlyList<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var index = 0;
        var line = 1;
        var column = 1;

        while (index < input.Length)
        {
            var current = input[index];

            if (char.IsWhiteSpace(current))
            {
                Advance(input, ref index, ref line, ref column);
                continue;
            }

            var position = new SourcePosition(line, column);

            if (IsAsciiDigit(current))
            {
                var start = index;
                while (index < input.Length && IsAsciiDigit(input[index]))
                {
                    Advance(input, ref index, ref line, ref column);
                }

                tokens.Add(new Token(TokenType.Number, input[start..index], position));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = index;
                while (index < input.Length && (char.IsLetterOrDigit(input[index]) || input[index] == '_'))
                {
                    Advance(input, ref index, ref line, ref column);
                }

                tokens.Add(new Token(TokenType.Identifier, input[start..index], position));
                continue;
            }

            var tokenType = current switch
            {
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Multiply,
                '/' => TokenType.Divide,
                '(' => TokenType.LeftParenthesis,
                ')' => TokenType.RightParenthesis,
                _ => throw new LexicalException(current, position)
            };

            tokens.Add(new Token(tokenType, current.ToString(), position));
            Advance(input, ref index, ref line, ref column);
        }

        tokens.Add(new Token(TokenType.EndOfInput, string.Empty, new SourcePosition(line, column)));
        return tokens;
    }

    private static bool IsAsciiDigit(char character) => character is >= '0' and <= '9';

    private static void Advance(string input, ref int index, ref int line, ref int column)
    {
        if (input[index] == '\r')
        {
            index++;
            if (index < input.Length && input[index] == '\n')
            {
                index++;
            }

            line++;
            column = 1;
            return;
        }

        if (input[index] == '\n')
        {
            index++;
            line++;
            column = 1;
            return;
        }

        index++;
        column++;
    }
}
