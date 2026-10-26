using FormalLanguageTools.FiniteAutomata.Models;

namespace FormalLanguageTools.FiniteAutomata.Results;

public sealed class AutomatonMinimizationResult
{
    public AutomatonMinimizationResult(DeterministicFiniteAutomaton deterministicAutomaton, DeterministicFiniteAutomaton minimizedAutomaton, IEnumerable<string> trace)
    {
        ArgumentNullException.ThrowIfNull(deterministicAutomaton);
        ArgumentNullException.ThrowIfNull(minimizedAutomaton);
        ArgumentNullException.ThrowIfNull(trace);
        DeterministicAutomaton = deterministicAutomaton;
        MinimizedAutomaton = minimizedAutomaton;
        Trace = Array.AsReadOnly(trace.ToArray());
    }

    public DeterministicFiniteAutomaton DeterministicAutomaton { get; }

    public DeterministicFiniteAutomaton MinimizedAutomaton { get; }

    public IReadOnlyList<string> Trace { get; }
}
