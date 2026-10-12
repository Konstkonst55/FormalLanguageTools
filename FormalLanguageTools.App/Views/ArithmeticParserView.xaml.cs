using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FormalLanguageTools.App.Models;
using FormalLanguageTools.App.ViewModels;

namespace FormalLanguageTools.App.Views;

public partial class ArithmeticParserView : UserControl
{
    private readonly ErrorHighlightRenderer _errorHighlightRenderer;
    private ArithmeticParserViewModel? _viewModel;
    private bool _synchronizingEditor;

    public ArithmeticParserView()
    {
        InitializeComponent();
        ExpressionEditor.TextArea.TextView.LineTransformers.Add(new ArithmeticSyntaxColorizer());
        _errorHighlightRenderer = new ErrorHighlightRenderer(ExpressionEditor.TextArea.TextView);
        ExpressionEditor.TextArea.TextView.BackgroundRenderers.Add(_errorHighlightRenderer);
        ExpressionEditor.TextChanged += ExpressionEditor_OnTextChanged;
        DataContextChanged += ArithmeticParserView_OnDataContextChanged;
    }

    private void ArithmeticParserView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.Diagnostics.CollectionChanged -= Diagnostics_CollectionChanged;
        }

        _viewModel = e.NewValue as ArithmeticParserViewModel;
        if (_viewModel is null)
        {
            _errorHighlightRenderer.SetErrorLocations(Array.Empty<SourceLocation>());
            return;
        }

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.Diagnostics.CollectionChanged += Diagnostics_CollectionChanged;
        SynchronizeEditor(_viewModel.EditorText);
        UpdateErrorHighlights();
    }

    private void ExpressionEditor_OnTextChanged(object? sender, EventArgs e)
    {
        if (!_synchronizingEditor && _viewModel is not null)
        {
            _viewModel.EditorText = ExpressionEditor.Text;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.PropertyName == nameof(ArithmeticParserViewModel.EditorText))
        {
            SynchronizeEditor(_viewModel.EditorText);
        }
    }

    private void Diagnostics_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateErrorHighlights();
    }

    private void SynchronizeEditor(string text)
    {
        if (ExpressionEditor.Text == text)
        {
            return;
        }

        _synchronizingEditor = true;
        try
        {
            ExpressionEditor.Text = text;
            ExpressionEditor.CaretOffset = ExpressionEditor.Document.TextLength;
        }
        finally
        {
            _synchronizingEditor = false;
        }
    }

    private void UpdateErrorHighlights()
    {
        if (_viewModel is null)
        {
            _errorHighlightRenderer.SetErrorLocations(Array.Empty<SourceLocation>());
            return;
        }

        var locations = _viewModel.Diagnostics
            .Where(diagnostic => diagnostic.IsError)
            .Select(diagnostic => new SourceLocation(diagnostic.LineNumber, diagnostic.Column));
        _errorHighlightRenderer.SetErrorLocations(locations);
    }

    private void DiagnosticsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiagnosticsGrid.SelectedItem is not ExpressionDiagnostic diagnostic)
        {
            return;
        }

        var lineNumber = Math.Clamp(diagnostic.LineNumber, 1, ExpressionEditor.Document.LineCount);
        var line = ExpressionEditor.Document.GetLineByNumber(lineNumber);
        var caretColumn = Math.Clamp(diagnostic.Column - 1, 0, line.Length);
        var caretOffset = line.Offset + caretColumn;
        ExpressionEditor.CaretOffset = caretOffset;

        if (diagnostic.IsError && line.Length > 0)
        {
            var highlightedOffset = line.Offset + Math.Clamp(diagnostic.Column - 1, 0, line.Length - 1);
            ExpressionEditor.Select(highlightedOffset, 1);
        }
        else
        {
            ExpressionEditor.Select(caretOffset, 0);
        }

        ExpressionEditor.ScrollToLine(lineNumber);
        ExpressionEditor.Focus();
    }
}
