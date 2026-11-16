using FormalLanguageTools.PushdownAutomata.Models;

namespace FormalLanguageTools.App.Models;

public sealed class PushdownAutomatonResultRow
{
    public PushdownAutomatonResultRow(RecognitionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Result = result;
        Word = result.Input.Length == 0 ? "e" : result.Input;
        Status = FormatStatus(result.Status);
        FinalState = result.FinalState;
        RemainingInput = FormatRemainingInput(result.Input, result.InputPosition);
        Stack = result.Stack.Length == 0 ? "e" : result.Stack;
        StepCount = result.TotalSteps;
        Reason = result.Reason;
    }

    public RecognitionResult Result { get; }

    public string Word { get; }

    public string Status { get; }

    public string FinalState { get; }

    public string RemainingInput { get; }

    public string Stack { get; }

    public int StepCount { get; }

    public string Reason { get; }

    public static string FormatStatus(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Accepted => "Принята",
            ExecutionStatus.Rejected => "Отклонена",
            ExecutionStatus.InvalidInput => "Неверный символ",
            ExecutionStatus.NonTerminatingLoop => "Обнаружен цикл",
            ExecutionStatus.StepLimitExceeded => "Лимит шагов",
            _ => status.ToString()
        };
    }

    public static string FormatRemainingInput(string input, int position)
    {
        var remaining = input[position..];
        return remaining.Length == 0 ? "e" : remaining;
    }
}
