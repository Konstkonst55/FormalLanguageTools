namespace FormalLanguageTools.PushdownAutomata.Models;

public sealed class RecognitionResult
{
    public RecognitionResult(string input, ExecutionStatus status, string finalState, int inputPosition, string stack, int totalSteps, IEnumerable<ExecutionStep> steps, bool isTraceTruncated, string reason)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(finalState);
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentOutOfRangeException.ThrowIfNegative(totalSteps);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(reason);

        Input = input;
        Status = status;
        FinalState = finalState;
        InputPosition = inputPosition;
        Stack = stack;
        TotalSteps = totalSteps;
        Steps = Array.AsReadOnly(steps.ToArray());
        IsTraceTruncated = isTraceTruncated;
        Reason = reason;
    }

    public string Input { get; }

    public ExecutionStatus Status { get; }

    public bool IsAccepted => Status == ExecutionStatus.Accepted;

    public string FinalState { get; }

    public int InputPosition { get; }

    public string Stack { get; }

    public int TotalSteps { get; }

    public IReadOnlyList<ExecutionStep> Steps { get; }

    public bool IsTraceTruncated { get; }

    public string Reason { get; }
}
