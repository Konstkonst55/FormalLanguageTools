namespace FormalLanguageTools.FiniteAutomata.Models;

public sealed class NondeterministicFiniteAutomaton : FiniteAutomatonBase
{
    public NondeterministicFiniteAutomaton(IEnumerable<string> states, IEnumerable<string> alphabet, string initialState, IEnumerable<string> finalStates, IEnumerable<AutomatonTransition> transitions)
        : base(states, alphabet, initialState, finalStates, transitions)
    {
    }

    public IReadOnlyList<string> GetDestinations(string state, string symbol)
    {
        return Array.AsReadOnly(Transitions
            .Where(transition => transition.FromState == state && transition.Symbol == symbol)
            .Select(transition => transition.ToState)
            .Distinct(StringComparer.Ordinal)
            .ToArray());
    }
}
