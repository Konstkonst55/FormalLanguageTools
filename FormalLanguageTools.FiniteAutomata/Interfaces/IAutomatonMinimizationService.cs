using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Results;

namespace FormalLanguageTools.FiniteAutomata.Interfaces;

public interface IAutomatonMinimizationService
{
    AutomatonMinimizationResult Process(NondeterministicFiniteAutomaton automaton, bool captureTrace = false);
}
