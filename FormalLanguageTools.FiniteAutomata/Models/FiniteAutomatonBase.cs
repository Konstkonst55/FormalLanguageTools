namespace FormalLanguageTools.FiniteAutomata.Models;

public abstract class FiniteAutomatonBase : IFiniteAutomaton
{
    protected FiniteAutomatonBase(IEnumerable<string> states, IEnumerable<string> alphabet, string initialState, IEnumerable<string> finalStates, IEnumerable<AutomatonTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(alphabet);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(finalStates);
        ArgumentNullException.ThrowIfNull(transitions);

        var stateList = states.ToArray();
        var alphabetList = alphabet.ToArray();
        var transitionList = transitions.Distinct().ToArray();
        var finalSet = new HashSet<string>(finalStates, StringComparer.Ordinal);

        if (stateList.Length == 0 || stateList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Автомат должен содержать хотя бы одно состояние с непустым именем.", nameof(states));
        }

        if (stateList.Distinct(StringComparer.Ordinal).Count() != stateList.Length)
        {
            throw new ArgumentException("Имена состояний должны быть уникальны.", nameof(states));
        }

        if (alphabetList.Length == 0 || alphabetList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Алфавит должен содержать хотя бы один непустой символ.", nameof(alphabet));
        }

        if (alphabetList.Distinct(StringComparer.Ordinal).Count() != alphabetList.Length)
        {
            throw new ArgumentException("Символы алфавита должны быть уникальны.", nameof(alphabet));
        }

        if (!stateList.Contains(initialState, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Начальное состояние '{initialState}' отсутствует в списке состояний.", nameof(initialState));
        }

        var stateSet = new HashSet<string>(stateList, StringComparer.Ordinal);
        if (finalSet.Any(state => !stateSet.Contains(state)))
        {
            throw new ArgumentException("Множество конечных состояний содержит неизвестные состояния.", nameof(finalStates));
        }

        var alphabetSet = new HashSet<string>(alphabetList, StringComparer.Ordinal);
        foreach (var transition in transitionList)
        {
            if (!stateSet.Contains(transition.FromState) || !stateSet.Contains(transition.ToState))
            {
                throw new ArgumentException($"Переход '{transition.FromState}' -> '{transition.ToState}' ссылается на неизвестное состояние.", nameof(transitions));
            }

            if (!alphabetSet.Contains(transition.Symbol))
            {
                throw new ArgumentException($"Символ '{transition.Symbol}' отсутствует в алфавите.", nameof(transitions));
            }
        }

        States = Array.AsReadOnly(stateList);
        Alphabet = Array.AsReadOnly(alphabetList);
        InitialState = initialState;
        FinalStates = Array.AsReadOnly(stateList.Where(finalSet.Contains).ToArray());
        Transitions = Array.AsReadOnly(transitionList);
    }

    public IReadOnlyList<string> States { get; }

    public IReadOnlyList<string> Alphabet { get; }

    public string InitialState { get; }

    public IReadOnlyList<string> FinalStates { get; }

    public IReadOnlyList<AutomatonTransition> Transitions { get; }
}
