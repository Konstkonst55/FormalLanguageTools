using System.Windows;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.ArithmeticParser.Classes;
using FormalLanguageTools.FiniteAutomata.Services;

namespace FormalLanguageTools.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = new MainWindow(new ArithmeticExpressionParser(), new WpfTextFileDialogService(), new AutomatonMinimizationService(), new WpfAutomatonFileDialogService());
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
