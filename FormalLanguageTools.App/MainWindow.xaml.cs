using System.Windows;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.App.ViewModels;
using FormalLanguageTools.ArithmeticParser.Interfaces;
using FormalLanguageTools.FiniteAutomata.Interfaces;

namespace FormalLanguageTools.App;

public partial class MainWindow : Window
{
    public MainWindow(IArithmeticExpressionParser parser, ITextFileDialogService fileDialogService, IAutomatonMinimizationService automatonMinimizationService, IAutomatonFileDialogService automatonFileDialogService)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(automatonMinimizationService);
        ArgumentNullException.ThrowIfNull(automatonFileDialogService);
        InitializeComponent();
        ArithmeticParserViewControl.DataContext = new ArithmeticParserViewModel(parser, fileDialogService);
        FiniteAutomataViewControl.DataContext = new FiniteAutomataViewModel(automatonMinimizationService, automatonFileDialogService);
    }
}
