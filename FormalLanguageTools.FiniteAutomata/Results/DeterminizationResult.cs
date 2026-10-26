using FormalLanguageTools.FiniteAutomata.Models;

namespace FormalLanguageTools.FiniteAutomata.Results;

public sealed class DeterminizationResult
{
    public DeterminizationResult(DeterministicFiniteAutomaton automaton, IEnumerable<string> trace)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        ArgumentNullException.ThrowIfNull(trace);
        Automaton = automaton;
        Trace = Array.AsReadOnly(trace.ToArray());
    }

    public DeterministicFiniteAutomaton Automaton { get; }

    public IReadOnlyList<string> Trace { get; }
}
