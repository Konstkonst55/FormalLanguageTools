using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using FormalLanguageTools.App.Commands;
using FormalLanguageTools.App.Services;
using FormalLanguageTools.FiniteAutomata.Exceptions;
using FormalLanguageTools.FiniteAutomata.Formatting;
using FormalLanguageTools.FiniteAutomata.Interfaces;
using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Parsing;

namespace FormalLanguageTools.App.ViewModels;

public sealed class FiniteAutomataViewModel : INotifyPropertyChanged
{
    private const string Example = "State;0;1";
    private readonly IAutomatonMinimizationService _minimizationService;
    private readonly IAutomatonFileDialogService _fileDialogService;
    private readonly TransitionTableParser _parser = new();
    private readonly TransitionTableFormatter _formatter = new();
    private string _inputText = Example;
    private string _outputText = string.Empty;
    private string _status = "Пример из лекции загружен. Выполните преобразование.";
    private bool _captureTrace;
    private NondeterministicFiniteAutomaton? _inputAutomaton;
    private DeterministicFiniteAutomaton? _minimizedAutomaton;
    private IReadOnlyList<string> _trace = Array.Empty<string>();

    public FiniteAutomataViewModel(IAutomatonMinimizationService minimizationService, IAutomatonFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(minimizationService);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        _minimizationService = minimizationService;
        _fileDialogService = fileDialogService;
        OpenInputCommand = new RelayCommand(OpenInput);
        SaveInputCommand = new RelayCommand(SaveInput);
        SaveOutputCommand = new RelayCommand(SaveOutput, () => !string.IsNullOrWhiteSpace(OutputText));
        ProcessCommand = new RelayCommand(Process);
        RefreshInputAutomaton();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RelayCommand OpenInputCommand { get; }

    public RelayCommand SaveInputCommand { get; }

    public RelayCommand SaveOutputCommand { get; }

    public RelayCommand ProcessCommand { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (_inputText == value)
            {
                return;
            }

            _inputText = value ?? string.Empty;
            OnPropertyChanged();
            ResetResult();
            RefreshInputAutomaton();
        }
    }

    public string OutputText
    {
        get => _outputText;
        private set
        {
            if (_outputText == value)
            {
                return;
            }

            _outputText = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
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

    public bool CaptureTrace
    {
        get => _captureTrace;
        set
        {
            if (_captureTrace == value)
            {
                return;
            }

            _captureTrace = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanShowTrace));
        }
    }

    public bool CanShowTrace => CaptureTrace && _trace.Count > 0;

    public string TraceText => string.Join(Environment.NewLine, _trace);

    public NondeterministicFiniteAutomaton? InputAutomaton
    {
        get => _inputAutomaton;
        private set
        {
            if (ReferenceEquals(_inputAutomaton, value))
            {
                return;
            }

            _inputAutomaton = value;
            OnPropertyChanged();
        }
    }

    public DeterministicFiniteAutomaton? MinimizedAutomaton
    {
        get => _minimizedAutomaton;
        private set
        {
            if (ReferenceEquals(_minimizedAutomaton, value))
            {
                return;
            }

            _minimizedAutomaton = value;
            OnPropertyChanged();
        }
    }

    private void OpenInput()
    {
        try
        {
            var filePath = _fileDialogService.OpenInputFile();
            if (filePath is null)
            {
                return;
            }

            InputText = File.ReadAllText(filePath, Encoding.UTF8);
            if (InputAutomaton is not null)
            {
                Status = $"Загружен файл: {Path.GetFileName(filePath)}. Таблица корректна.";
            }
        }
        catch (Exception exception) when (IsFileException(exception))
        {
            Status = $"Не удалось прочитать файл: {exception.Message}";
        }
    }

    private void SaveInput()
    {
        SaveText(InputText, "automaton-input.txt", "Входная таблица сохранена.");
    }

    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            Status = "Нет выходной таблицы для сохранения.";
            return;
        }

        SaveText(OutputText, "minimized-dfa.txt", "Таблица минимизированного ДКА сохранена.");
    }

    private void SaveText(string text, string suggestedName, string successMessage)
    {
        try
        {
            var filePath = _fileDialogService.SaveFile(suggestedName);
            if (filePath is null)
            {
                return;
            }

            File.WriteAllText(filePath, text, new UTF8Encoding(false));
            Status = $"{successMessage} Файл: {Path.GetFileName(filePath)}.";
        }
        catch (Exception exception) when (IsFileException(exception))
        {
            Status = $"Не удалось сохранить файл: {exception.Message}";
        }
    }

    private void Process()
    {
        ResetResult();
        try
        {
            var parsedAutomaton = _parser.Parse(InputText);
            InputAutomaton = parsedAutomaton;
            var result = _minimizationService.Process(parsedAutomaton, CaptureTrace);
            MinimizedAutomaton = result.MinimizedAutomaton;
            OutputText = _formatter.Format(result.MinimizedAutomaton);
            _trace = result.Trace;
            OnPropertyChanged(nameof(TraceText));
            OnPropertyChanged(nameof(CanShowTrace));
            Status = $"Готово. НКА: {parsedAutomaton.States.Count} состояний; ДКА: {result.DeterministicAutomaton.States.Count}; минимизированный ДКА: {result.MinimizedAutomaton.States.Count}.";
        }
        catch (AutomatonFormatException exception)
        {
            InputAutomaton = null;
            Status = exception.Message;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            InputAutomaton = null;
            Status = $"Не удалось обработать автомат: {exception.Message}";
        }
    }

    private void ResetResult()
    {
        OutputText = string.Empty;
        MinimizedAutomaton = null;
        _trace = Array.Empty<string>();
        OnPropertyChanged(nameof(TraceText));
        OnPropertyChanged(nameof(CanShowTrace));
    }

    private void RefreshInputAutomaton()
    {
        try
        {
            InputAutomaton = _parser.Parse(InputText);
            Status = $"Таблица корректна. Состояний: {InputAutomaton.States.Count}; символов алфавита: {InputAutomaton.Alphabet.Count}.";
        }
        catch (AutomatonFormatException exception)
        {
            InputAutomaton = null;
            Status = exception.Message;
        }
        catch (ArgumentException exception)
        {
            InputAutomaton = null;
            Status = $"Некорректная таблица: {exception.Message}";
        }
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
