namespace FormalLanguageTools.PushdownAutomata.Models;

public enum ExecutionStatus
{
    Accepted,
    Rejected,
    InvalidInput,
    NonTerminatingLoop,
    StepLimitExceeded
}
