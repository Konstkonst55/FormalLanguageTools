using FormalLanguageTools.FiniteAutomata.Algorithms;
using FormalLanguageTools.FiniteAutomata.Interfaces;
using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Results;

namespace FormalLanguageTools.FiniteAutomata.Services;

public sealed class AutomatonMinimizationService : IAutomatonMinimizationService
{
    private readonly NfaToDfaConverter _converter;
    private readonly DfaMinimizer _minimizer;

    public AutomatonMinimizationService(NfaToDfaConverter? converter = null, DfaMinimizer? minimizer = null)
    {
        _converter = converter ?? new NfaToDfaConverter();
        _minimizer = minimizer ?? new DfaMinimizer();
    }

    public AutomatonMinimizationResult Process(NondeterministicFiniteAutomaton automaton, bool captureTrace = false)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        var determinization = _converter.Convert(automaton, captureTrace);
        var minimization = _minimizer.Minimize(determinization.Automaton, captureTrace);
        var trace = captureTrace ? determinization.Trace.Concat(minimization.Trace).ToArray() : Array.Empty<string>();
        return new AutomatonMinimizationResult(determinization.Automaton, minimization.Automaton, trace);
    }
}
