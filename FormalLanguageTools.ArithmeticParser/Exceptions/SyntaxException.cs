using FormalLanguageTools.Shared.DataClasses;
using FormalLanguageTools.Shared.Exceptions;

namespace FormalLanguageTools.ArithmeticParser.Exceptions;

public sealed class SyntaxException : FormalLanguageException
{
    public SyntaxException(SourcePosition position, IReadOnlyList<string> expectedTokens, string actualToken)
        : base($"Ожидалось: {FormatExpected(expectedTokens)}; получено: {actualToken}.", position)
    {
        ArgumentNullException.ThrowIfNull(actualToken);
        ExpectedTokens = Array.AsReadOnly(expectedTokens.ToArray());
        ActualToken = actualToken;
    }

    public IReadOnlyList<string> ExpectedTokens { get; }

    public string ActualToken { get; }

    private static string FormatExpected(IReadOnlyList<string> expectedTokens)
    {
        ArgumentNullException.ThrowIfNull(expectedTokens);

        return expectedTokens.Count switch
        {
            0 => "корректный токен",
            1 => expectedTokens[0],
            2 => $"{expectedTokens[0]} или {expectedTokens[1]}",
            _ => $"{string.Join(", ", expectedTokens.Take(expectedTokens.Count - 1))} или {expectedTokens[expectedTokens.Count - 1]}"
        };
    }
}
