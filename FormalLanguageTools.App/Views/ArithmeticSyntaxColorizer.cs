using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace FormalLanguageTools.App.Views;

public sealed class ArithmeticSyntaxColorizer : DocumentColorizingTransformer
{
    private static readonly Regex TokenPattern = new(
        @"(?<Number>\d+)|(?<Identifier>[\p{L}_][\p{L}\p{Nd}_]*)|(?<Operator>[+\-*/])|(?<Parenthesis>[()])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        foreach (Match match in TokenPattern.Matches(text))
        {
            var brush = match.Groups["Number"].Success
                ? Brushes.SteelBlue
                : match.Groups["Identifier"].Success
                    ? Brushes.DarkCyan
                    : match.Groups["Operator"].Success
                        ? Brushes.DarkOrange
                        : Brushes.MediumPurple;
            var startOffset = line.Offset + match.Index;
            var endOffset = startOffset + match.Length;
            ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetForegroundBrush(brush));
        }
    }
}
