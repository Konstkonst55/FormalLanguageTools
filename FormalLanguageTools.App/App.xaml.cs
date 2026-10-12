using System.Windows;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.ArithmeticParser.Classes;

namespace FormalLanguageTools.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = new MainWindow(new ArithmeticExpressionParser(), new WpfTextFileDialogService());
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
