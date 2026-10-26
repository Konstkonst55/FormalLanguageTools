using FormalLanguageTools.FiniteAutomata.Models;

namespace FormalLanguageTools.FiniteAutomata.Formatting;

public sealed class TransitionTableFormatter
{
    public string Format(IFiniteAutomaton automaton)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        var lines = new List<string> { string.Join(";", new[] { "State" }.Concat(automaton.Alphabet)) };
        foreach (var state in automaton.States)
        {
            var prefix = state == automaton.InitialState ? "->" : string.Empty;
            if (automaton.FinalStates.Contains(state, StringComparer.Ordinal))
            {
                prefix += "*";
            }

            var cells = new List<string> { prefix + state };
            foreach (var symbol in automaton.Alphabet)
            {
                var destinations = automaton.Transitions
                    .Where(transition => transition.FromState == state && transition.Symbol == symbol)
                    .Select(transition => transition.ToState)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                cells.Add(destinations.Length == 0 ? "-" : string.Join(",", destinations));
            }

            lines.Add(string.Join(";", cells));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
