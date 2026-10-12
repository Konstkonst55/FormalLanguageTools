using System.Windows;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.App.ViewModels;
using FormalLanguageTools.ArithmeticParser.Interfaces;

namespace FormalLanguageTools.App;

public partial class MainWindow : Window
{
    public MainWindow(IArithmeticExpressionParser parser, ITextFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        InitializeComponent();
        ArithmeticParserViewControl.DataContext = new ArithmeticParserViewModel(parser, fileDialogService);
    }
}
