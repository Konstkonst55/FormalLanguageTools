using FormalLanguageTools.Shared.DataClasses;
using FormalLanguageTools.Shared.Exceptions;

namespace FormalLanguageTools.ArithmeticParser.Exceptions;

public sealed class LexicalException : FormalLanguageException
{
    public LexicalException(char character, SourcePosition position)
        : base($"Недопустимый символ '{character}'.", position)
    {
        Character = character;
    }

    public char Character { get; }
}
