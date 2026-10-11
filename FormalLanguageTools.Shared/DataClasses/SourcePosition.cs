namespace FormalLanguageTools.Shared.DataClasses;

public readonly record struct SourcePosition(int Line, int Column)
{
    public override string ToString() => $"{Line}:{Column}";
}
