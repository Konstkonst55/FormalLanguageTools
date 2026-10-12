using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using FormalLanguageTools.App.Commands;
using FormalLanguageTools.App.Models;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.ArithmeticParser.Exceptions;
using FormalLanguageTools.ArithmeticParser.Interfaces;
using FormalLanguageTools.Shared.Exceptions;

namespace FormalLanguageTools.App.ViewModels;

public sealed class ArithmeticParserViewModel : INotifyPropertyChanged
{
    private readonly IArithmeticExpressionParser _parser;
    private readonly ITextFileDialogService _fileDialogService;
    private string _editorText = string.Empty;
    private string _status = "Готово к проверке.";
    private ExpressionDiagnostic? _selectedDiagnostic;

    public ArithmeticParserViewModel(IArithmeticExpressionParser parser, ITextFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        _parser = parser;
        _fileDialogService = fileDialogService;
        OpenFileCommand = new RelayCommand(OpenFile);
        CheckCommand = new RelayCommand(CheckExpressions);
        ClearCommand = new RelayCommand(Clear);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ExpressionDiagnostic> Diagnostics { get; } = new();

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (_editorText == value)
            {
                return;
            }

            _editorText = value ?? string.Empty;
            OnPropertyChanged();
            Diagnostics.Clear();
            SelectedDiagnostic = null;
            Status = string.IsNullOrWhiteSpace(_editorText) ? "Готово к проверке." : "Текст изменён. Запустите проверку.";
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public ExpressionDiagnostic? SelectedDiagnostic
    {
        get => _selectedDiagnostic;
        set
        {
            if (_selectedDiagnostic == value)
            {
                return;
            }

            _selectedDiagnostic = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand CheckCommand { get; }

    public RelayCommand ClearCommand { get; }

    private void OpenFile()
    {
        try
        {
            var text = _fileDialogService.OpenAndReadText();
            if (text is null)
            {
                return;
            }

            EditorText = text;
            Status = "Файл загружен. Нажмите «Проверить всё» для анализа.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            Status = $"Не удалось прочитать файл: {exception.Message}";
        }
    }

    private void CheckExpressions()
    {
        Diagnostics.Clear();
        SelectedDiagnostic = null;
        var lines = EditorText.Split('\n');
        var checkedCount = 0;
        var errorCount = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var expression = lines[index].EndsWith('\r') ? lines[index][..^1] : lines[index];
            if (string.IsNullOrWhiteSpace(expression))
            {
                continue;
            }

            checkedCount++;
            try
            {
                _parser.Parse(expression);
                Diagnostics.Add(new ExpressionDiagnostic(index + 1, 1, expression, "Корректно", "Синтаксических ошибок не обнаружено.", false));
            }
            catch (FormalLanguageException exception)
            {
                errorCount++;
                var column = Math.Max(1, exception.Position.Column);
                var status = exception is LexicalException ? "Лексическая ошибка" : "Синтаксическая ошибка";
                Diagnostics.Add(new ExpressionDiagnostic(index + 1, column, expression, status, exception.Message, true));
            }
        }

        if (checkedCount == 0)
        {
            Status = "Нет непустых выражений для проверки.";
            return;
        }

        Status = $"Проверено выражений: {checkedCount}. Ошибок: {errorCount}.";
        SelectedDiagnostic = Diagnostics.FirstOrDefault(diagnostic => diagnostic.IsError) ?? Diagnostics.FirstOrDefault();
    }

    private void Clear()
    {
        EditorText = string.Empty;
        Diagnostics.Clear();
        SelectedDiagnostic = null;
        Status = "Редактор очищен.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
