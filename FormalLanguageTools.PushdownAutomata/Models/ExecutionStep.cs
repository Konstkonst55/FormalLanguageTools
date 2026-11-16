namespace FormalLanguageTools.PushdownAutomata.Models;

public sealed record ExecutionStep(int Number, string StateBefore, int InputPositionBefore, string StackBefore, char? ConsumedSymbol, string StateAfter, string StackAfter);
