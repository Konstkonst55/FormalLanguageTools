namespace FormalLanguageTools.FiniteAutomata.Exceptions;

public sealed class AutomatonFormatException : Exception
{
    public AutomatonFormatException(string message, int lineNumber = 0) : base(lineNumber > 0 ? $"Строка {lineNumber}: {message}" : message)
    {
        LineNumber = lineNumber;
    }

    public int LineNumber { get; }
}
