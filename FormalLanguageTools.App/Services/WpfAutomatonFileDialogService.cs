using Microsoft.Win32;

namespace FormalLanguageTools.App.Services;

public sealed class WpfAutomatonFileDialogService : IAutomatonFileDialogService
{
    public string? OpenInputFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть таблицу переходов",
            Filter = "Таблицы автоматов (*.txt;*.csv)|*.txt;*.csv|Все файлы (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(string suggestedFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить таблицу переходов",
            Filter = "Текстовые файлы (*.txt)|*.txt|CSV-файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = suggestedFileName,
            AddExtension = true,
            DefaultExt = ".txt",
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
