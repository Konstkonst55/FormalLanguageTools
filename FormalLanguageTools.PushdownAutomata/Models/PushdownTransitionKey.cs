namespace FormalLanguageTools.PushdownAutomata.Models;

public readonly record struct PushdownTransitionKey(string State, char? InputSymbol, char StackTop);
