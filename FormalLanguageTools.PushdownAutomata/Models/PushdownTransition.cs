namespace FormalLanguageTools.PushdownAutomata.Models;

public sealed record PushdownTransition(string State, char? InputSymbol, char StackTop, string NextState, string Replacement);
