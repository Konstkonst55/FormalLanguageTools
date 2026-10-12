using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using FormalLanguageTools.App.Models;

namespace FormalLanguageTools.App.Views;

public sealed class ErrorHighlightRenderer : IBackgroundRenderer
{
    private static readonly Brush ErrorBackground = CreateBackgroundBrush();
    private static readonly Brush ErrorUnderline = Brushes.Crimson;
    private readonly TextView _textView;
    private IReadOnlyList<SourceLocation> _locations = Array.Empty<SourceLocation>();

    public ErrorHighlightRenderer(TextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        _textView = textView;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetErrorLocations(IEnumerable<SourceLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);
        _locations = locations.ToArray();
        _textView.Redraw();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || _locations.Count == 0)
        {
            return;
        }

        textView.EnsureVisualLines();
        foreach (var location in _locations)
        {
            if (location.LineNumber < 1 || location.LineNumber > textView.Document.LineCount)
            {
                continue;
            }

            var line = textView.Document.GetLineByNumber(location.LineNumber);
            if (line.Length == 0)
            {
                continue;
            }

            var columnOffset = Math.Clamp(location.Column - 1, 0, line.Length - 1);
            var segment = new TextSegment { StartOffset = line.Offset + columnOffset, Length = 1 };
            foreach (var rectangle in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.DrawRectangle(ErrorBackground, null, rectangle);
                var underline = new Rect(rectangle.Left, Math.Max(rectangle.Top, rectangle.Bottom - 2), Math.Max(rectangle.Width, 3), 2);
                drawingContext.DrawRectangle(ErrorUnderline, null, underline);
            }
        }
    }

    private static Brush CreateBackgroundBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(45, 220, 38, 38));
        brush.Freeze();
        return brush;
    }
}
