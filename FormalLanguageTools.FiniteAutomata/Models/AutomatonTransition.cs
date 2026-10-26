namespace FormalLanguageTools.FiniteAutomata.Models;

public sealed record AutomatonTransition(string FromState, string Symbol, string ToState);
