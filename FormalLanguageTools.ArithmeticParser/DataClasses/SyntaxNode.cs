namespace FormalLanguageTools.ArithmeticParser.DataClasses;

public sealed class SyntaxNode
{
    public SyntaxNode(string name, string? value = null, IEnumerable<SyntaxNode>? children = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Имя узла не может быть пустым.", nameof(name));
        }

        Name = name;
        Value = value;
        Children = Array.AsReadOnly((children ?? Array.Empty<SyntaxNode>()).ToArray());
    }

    public string Name { get; }

    public string? Value { get; }

    public IReadOnlyList<SyntaxNode> Children { get; }

    public string DisplayText => Value is null ? Name : $"{Name}: {Value}";

    public override string ToString() => DisplayText;
}
