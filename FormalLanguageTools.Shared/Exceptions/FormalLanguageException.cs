using FormalLanguageTools.Shared.DataClasses;

namespace FormalLanguageTools.Shared.Exceptions;

public abstract class FormalLanguageException : Exception
{
    protected FormalLanguageException(string message, SourcePosition position) : base(message)
    {
        Position = position;
    }

    public SourcePosition Position { get; }
}
