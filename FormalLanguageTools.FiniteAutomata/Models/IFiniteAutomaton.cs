namespace FormalLanguageTools.FiniteAutomata.Models;

public interface IFiniteAutomaton
{
    IReadOnlyList<string> States { get; }

    IReadOnlyList<string> Alphabet { get; }

    string InitialState { get; }

    IReadOnlyList<string> FinalStates { get; }

    IReadOnlyList<AutomatonTransition> Transitions { get; }
}
