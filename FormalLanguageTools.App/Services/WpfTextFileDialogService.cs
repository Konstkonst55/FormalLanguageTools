using Microsoft.Win32;
using System.IO;

namespace FormalLanguageTools.App.Services;

public sealed class WpfTextFileDialogService : ITextFileDialogService
{
    public string? OpenAndReadText()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть файл с выражениями",
            Filter = "Текстовые файлы (*.txt;*.expr)|*.txt;*.expr|Все файлы (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? File.ReadAllText(dialog.FileName) : null;
    }
}
