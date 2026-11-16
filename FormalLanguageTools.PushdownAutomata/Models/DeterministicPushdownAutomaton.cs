using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace FormalLanguageTools.PushdownAutomata.Models;

public sealed class DeterministicPushdownAutomaton
{
    private readonly IReadOnlyDictionary<PushdownTransitionKey, PushdownTransition> _transitions;
    private readonly IReadOnlyCollection<PushdownTransition> _transitionValues;
    private readonly IReadOnlySet<string> _finalStates;
    private readonly IReadOnlySet<char> _inputAlphabet;
    private readonly IReadOnlySet<string> _states;

    public DeterministicPushdownAutomaton(string initialState, char initialStackSymbol, IEnumerable<string> finalStates, IEnumerable<PushdownTransition> transitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialState);
        ArgumentNullException.ThrowIfNull(finalStates);
        ArgumentNullException.ThrowIfNull(transitions);

        if (char.IsWhiteSpace(initialStackSymbol) || initialStackSymbol == ';')
        {
            throw new ArgumentException("Начальный символ стека должен быть непробельным символом.", nameof(initialStackSymbol));
        }

        InitialState = initialState;
        InitialStackSymbol = initialStackSymbol;

        var finalStateSet = new HashSet<string>(finalStates, StringComparer.Ordinal);
        var transitionDictionary = new Dictionary<PushdownTransitionKey, PushdownTransition>();
        var stateSet = new HashSet<string>(StringComparer.Ordinal) { initialState };
        var inputAlphabetSet = new HashSet<char>();

        foreach (var finalState in finalStateSet)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(finalState);
            stateSet.Add(finalState);
        }

        foreach (var transition in transitions)
        {
            ArgumentNullException.ThrowIfNull(transition);
            ArgumentException.ThrowIfNullOrWhiteSpace(transition.State);
            ArgumentException.ThrowIfNullOrWhiteSpace(transition.NextState);
            ArgumentNullException.ThrowIfNull(transition.Replacement);
            if (char.IsWhiteSpace(transition.StackTop) || transition.StackTop == ';' || transition.InputSymbol.HasValue && (char.IsWhiteSpace(transition.InputSymbol.Value) || transition.InputSymbol.Value == ';') || transition.Replacement.Any(char.IsWhiteSpace) || transition.Replacement.Contains(';'))
            {
                throw new ArgumentException("Переход содержит недопустимые символы.", nameof(transitions));
            }

            var key = new PushdownTransitionKey(transition.State, transition.InputSymbol, transition.StackTop);
            if (!transitionDictionary.TryAdd(key, transition))
            {
                throw new ArgumentException($"Для состояния '{transition.State}', входного символа '{FormatInputSymbol(transition.InputSymbol)}' и вершины стека '{transition.StackTop}' задано более одного перехода.", nameof(transitions));
            }

            stateSet.Add(transition.State);
            stateSet.Add(transition.NextState);
            if (transition.InputSymbol.HasValue)
            {
                inputAlphabetSet.Add(transition.InputSymbol.Value);
            }
        }

        _transitions = new ReadOnlyDictionary<PushdownTransitionKey, PushdownTransition>(transitionDictionary);
        _transitionValues = Array.AsReadOnly(transitionDictionary.Values.ToArray());
        _finalStates = finalStateSet.ToFrozenSet(StringComparer.Ordinal);
        _inputAlphabet = inputAlphabetSet.ToFrozenSet();
        _states = stateSet.ToFrozenSet(StringComparer.Ordinal);
    }

    public string InitialState { get; }

    public char InitialStackSymbol { get; }

    public IReadOnlySet<string> FinalStates => _finalStates;

    public IReadOnlySet<char> InputAlphabet => _inputAlphabet;

    public IReadOnlySet<string> States => _states;

    public IReadOnlyCollection<PushdownTransition> Transitions => _transitionValues;

    public bool IsFinalState(string state)
    {
        return _finalStates.Contains(state);
    }

    public bool TryGetTransition(string state, char? inputSymbol, char stackTop, out PushdownTransition? transition)
    {
        return _transitions.TryGetValue(new PushdownTransitionKey(state, inputSymbol, stackTop), out transition);
    }

    private static string FormatInputSymbol(char? inputSymbol)
    {
        return inputSymbol?.ToString() ?? "-";
    }
}
