using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using FormalLanguageTools.App.Commands;
using FormalLanguageTools.App.Models;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.PushdownAutomata.Exceptions;
using FormalLanguageTools.PushdownAutomata.Models;
using FormalLanguageTools.PushdownAutomata.Parsing;
using FormalLanguageTools.PushdownAutomata.Simulation;

namespace FormalLanguageTools.App.ViewModels;

public sealed class PushdownAutomatonViewModel : INotifyPropertyChanged
{
    private const string DefaultRules = "InitialStack;Z\nState;Input;StackTop;NextState;ReplaceTop";
    private const string DefaultInputWords = "";

    private readonly PushdownAutomatonParser _parser;
    private readonly PushdownAutomatonSimulator _simulator;
    private readonly IAutomatonFileDialogService _fileDialogService;
    private string _rulesText = DefaultRules;
    private string _inputWordsText = DefaultInputWords;
    private string _status = "Загрузите пример из samples\\pushdown или введите описание автомата.";
    private string _traceText = string.Empty;
    private PushdownAutomatonResultRow? _selectedResult;

    public PushdownAutomatonViewModel(PushdownAutomatonParser parser, PushdownAutomatonSimulator simulator, IAutomatonFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(simulator);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        _parser = parser;
        _simulator = simulator;
        _fileDialogService = fileDialogService;
        OpenRulesCommand = new RelayCommand(OpenRules);
        SaveRulesCommand = new RelayCommand(SaveRules);
        CheckCommand = new RelayCommand(CheckWords);
        ClearCommand = new RelayCommand(Clear);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PushdownAutomatonResultRow> Results { get; } = new();

    public RelayCommand OpenRulesCommand { get; }

    public RelayCommand SaveRulesCommand { get; }

    public RelayCommand CheckCommand { get; }

    public RelayCommand ClearCommand { get; }

    public string RulesText
    {
        get => _rulesText;
        set
        {
            if (_rulesText == value)
            {
                return;
            }

            _rulesText = value ?? string.Empty;
            OnPropertyChanged();
            ResetResults();
            Status = "Описание автомата изменено. Запустите проверку.";
        }
    }

    public string InputWordsText
    {
        get => _inputWordsText;
        set
        {
            if (_inputWordsText == value)
            {
                return;
            }

            _inputWordsText = value ?? string.Empty;
            OnPropertyChanged();
            ResetResults();
            Status = "Список цепочек изменён. Запустите проверку.";
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

    public string TraceText
    {
        get => _traceText;
        private set
        {
            if (_traceText == value)
            {
                return;
            }

            _traceText = value;
            OnPropertyChanged();
        }
    }

    public PushdownAutomatonResultRow? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (ReferenceEquals(_selectedResult, value))
            {
                return;
            }

            _selectedResult = value;
            OnPropertyChanged();
            TraceText = value is null ? string.Empty : FormatTrace(value.Result);
        }
    }

    private void OpenRules()
    {
        try
        {
            var filePath = _fileDialogService.OpenInputFile();
            if (filePath is null)
            {
                return;
            }

            RulesText = File.ReadAllText(filePath, Encoding.UTF8);
            Status = $"Загружен файл: {Path.GetFileName(filePath)}.";
        }
        catch (Exception exception) when (IsFileException(exception))
        {
            Status = $"Не удалось прочитать файл: {exception.Message}";
        }
    }

    private void SaveRules()
    {
        try
        {
            var filePath = _fileDialogService.SaveFile("pushdown-automaton.txt");
            if (filePath is null)
            {
                return;
            }

            File.WriteAllText(filePath, RulesText, new UTF8Encoding(false));
            Status = $"Описание автомата сохранено: {Path.GetFileName(filePath)}.";
        }
        catch (Exception exception) when (IsFileException(exception))
        {
            Status = $"Не удалось сохранить файл: {exception.Message}";
        }
    }

    private void CheckWords()
    {
        ResetResults();

        try
        {
            var automaton = _parser.Parse(RulesText);
            var words = ParseInputWords(InputWordsText);
            foreach (var word in words)
            {
                Results.Add(new PushdownAutomatonResultRow(_simulator.Simulate(automaton, word)));
            }

            if (Results.Count == 0)
            {
                Status = "Не задано ни одной входной цепочки.";
                return;
            }

            var acceptedCount = Results.Count(result => result.Result.IsAccepted);
            var rejectedCount = Results.Count(result => result.Result.Status is ExecutionStatus.Rejected or ExecutionStatus.InvalidInput);
            var incompleteCount = Results.Count - acceptedCount - rejectedCount;
            Status = $"Проверено цепочек: {Results.Count}. Принято: {acceptedCount}. Отклонено: {rejectedCount}. Не завершено: {incompleteCount}.";
            SelectedResult = Results[0];
        }
        catch (PushdownAutomatonFormatException exception)
        {
            Status = exception.Message;
        }
        catch (ArgumentException exception)
        {
            Status = $"Некорректные параметры автомата: {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            Status = $"Не удалось выполнить проверку: {exception.Message}";
        }
    }

    private void Clear()
    {
        RulesText = string.Empty;
        InputWordsText = string.Empty;
        ResetResults();
        Status = "Редакторы и результаты очищены.";
    }

    private void ResetResults()
    {
        SelectedResult = null;
        Results.Clear();
        TraceText = string.Empty;
    }

    private static IReadOnlyList<string> ParseInputWords(string text)
    {
        var words = new List<string>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

        foreach (var line in lines)
        {
            var word = line.Trim();
            if (word.Length == 0)
            {
                continue;
            }

            if (word == "e")
            {
                word = string.Empty;
            }

            words.Add(word);
        }

        return words;
    }

    private static string FormatTrace(RecognitionResult result)
    {
        var output = new StringBuilder();
        var initialState = result.Steps.Count > 0 ? result.Steps[0].StateBefore : result.FinalState;
        var initialStack = result.Steps.Count > 0 ? result.Steps[0].StackBefore : result.Stack;
        output.AppendLine($"Начальная конфигурация: ({initialState}, {PushdownAutomatonResultRow.FormatRemainingInput(result.Input, 0)}, {FormatStack(initialStack)})");

        foreach (var step in result.Steps)
        {
            var remainingBefore = PushdownAutomatonResultRow.FormatRemainingInput(result.Input, step.InputPositionBefore);
            var nextPosition = step.InputPositionBefore + (step.ConsumedSymbol.HasValue ? 1 : 0);
            var remainingAfter = PushdownAutomatonResultRow.FormatRemainingInput(result.Input, nextPosition);
            var transitionLabel = step.ConsumedSymbol?.ToString() ?? "-";
            output.AppendLine($"{step.Number}. ({step.StateBefore}, {remainingBefore}, {FormatStack(step.StackBefore)}) --{transitionLabel}--> ({step.StateAfter}, {remainingAfter}, {FormatStack(step.StackAfter)})");
        }

        if (result.IsTraceTruncated)
        {
            output.AppendLine($"Показаны первые {result.Steps.Count} шагов из {result.TotalSteps}; подробный журнал ограничен для экономии памяти.");
        }

        output.AppendLine($"Итоговая конфигурация: ({result.FinalState}, {PushdownAutomatonResultRow.FormatRemainingInput(result.Input, result.InputPosition)}, {FormatStack(result.Stack)})");
        output.AppendLine($"Результат: {PushdownAutomatonResultRow.FormatStatus(result.Status)}.");
        output.Append(result.Reason);
        return output.ToString();
    }

    private static string FormatStack(string stack)
    {
        return stack.Length == 0 ? "e" : stack;
    }

    private static bool IsFileException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
