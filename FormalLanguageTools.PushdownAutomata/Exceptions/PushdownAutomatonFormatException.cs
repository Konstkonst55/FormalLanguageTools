namespace FormalLanguageTools.PushdownAutomata.Exceptions;

public sealed class PushdownAutomatonFormatException : FormatException
{
    public PushdownAutomatonFormatException(string message, int? lineNumber = null, Exception? innerException = null)
        : base(lineNumber.HasValue ? $"Строка {lineNumber.Value}: {message}" : message, innerException)
    {
        LineNumber = lineNumber;
    }

    public int? LineNumber { get; }
}
