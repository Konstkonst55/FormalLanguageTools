namespace FormalLanguageTools.FiniteAutomata.Models;

public sealed class DeterministicFiniteAutomaton : FiniteAutomatonBase
{
    public DeterministicFiniteAutomaton(IEnumerable<string> states, IEnumerable<string> alphabet, string initialState, IEnumerable<string> finalStates, IEnumerable<AutomatonTransition> transitions)
        : base(states, alphabet, initialState, finalStates, transitions)
    {
        var conflictingTransition = Transitions
            .GroupBy(transition => (transition.FromState, transition.Symbol))
            .FirstOrDefault(group => group.Select(transition => transition.ToState).Distinct(StringComparer.Ordinal).Count() > 1);

        if (conflictingTransition is not null)
        {
            throw new ArgumentException($"ДКА не может иметь несколько переходов из состояния '{conflictingTransition.Key.FromState}' по символу '{conflictingTransition.Key.Symbol}'.", nameof(transitions));
        }
    }

    public string? GetDestination(string state, string symbol)
    {
        return Transitions.FirstOrDefault(transition => transition.FromState == state && transition.Symbol == symbol)?.ToState;
    }
}
