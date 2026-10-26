using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.App.ViewModels;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;

namespace FormalLanguageTools.App.Views;

public partial class FiniteAutomataView : UserControl
{
    private readonly GraphViewer _inputGraphViewer = new();
    private readonly GraphViewer _outputGraphViewer = new();
    private readonly AutomatonGraphBuilder _graphBuilder = new();
    private FiniteAutomataViewModel? _viewModel;

    public FiniteAutomataView()
    {
        InitializeComponent();
        _inputGraphViewer.BindToPanel(InputGraphHost);
        _outputGraphViewer.BindToPanel(OutputGraphHost);
        _inputGraphViewer.GraphCanvas.Visibility = Visibility.Collapsed;
        _outputGraphViewer.GraphCanvas.Visibility = Visibility.Collapsed;
        DataContextChanged += FiniteAutomataView_OnDataContextChanged;
    }

    private void FiniteAutomataView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _viewModel = e.NewValue as FiniteAutomataViewModel;
        if (_viewModel is null)
        {
            UpdateInputGraph(null);
            UpdateOutputGraph(null);
            return;
        }

        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateInputGraph(_viewModel.InputAutomaton);
        UpdateOutputGraph(_viewModel.MinimizedAutomaton);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.PropertyName == nameof(FiniteAutomataViewModel.InputAutomaton))
        {
            UpdateInputGraph(_viewModel.InputAutomaton);
        }
        else if (e.PropertyName == nameof(FiniteAutomataViewModel.MinimizedAutomaton))
        {
            UpdateOutputGraph(_viewModel.MinimizedAutomaton);
        }
    }

    private void UpdateInputGraph(FormalLanguageTools.FiniteAutomata.Models.IFiniteAutomaton? automaton)
    {
        UpdateGraph(_inputGraphViewer, InputGraphPlaceholder, automaton);
    }

    private void UpdateOutputGraph(FormalLanguageTools.FiniteAutomata.Models.IFiniteAutomaton? automaton)
    {
        UpdateGraph(_outputGraphViewer, OutputGraphPlaceholder, automaton);
    }

    private void UpdateGraph(GraphViewer viewer, TextBlock placeholder, FormalLanguageTools.FiniteAutomata.Models.IFiniteAutomaton? automaton)
    {
        if (automaton is null)
        {
            viewer.GraphCanvas.Visibility = Visibility.Collapsed;
            placeholder.Visibility = Visibility.Visible;
            return;
        }

        viewer.GraphCanvas.Visibility = Visibility.Visible;
        viewer.Graph = _graphBuilder.CreateGraph(automaton);
        placeholder.Visibility = Visibility.Collapsed;
    }

    private void ShowTraceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null || !_viewModel.CanShowTrace)
        {
            return;
        }

        MessageBox.Show(Window.GetWindow(this), _viewModel.TraceText, "Трассировка преобразования и минимизации", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
