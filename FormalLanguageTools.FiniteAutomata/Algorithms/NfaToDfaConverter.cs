using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Results;

namespace FormalLanguageTools.FiniteAutomata.Algorithms;

public sealed class NfaToDfaConverter
{
    public DeterminizationResult Convert(NondeterministicFiniteAutomaton automaton, bool captureTrace = false)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        var trace = captureTrace ? new List<string>() : null;
        void AddTrace(string message) => trace?.Add(message);

        AddTrace("Этап 1. Детерминизация НКА методом построения подмножеств.");
        var originalStates = new HashSet<string>(automaton.States, StringComparer.Ordinal);
        var usedNames = new HashSet<string>(automaton.States, StringComparer.Ordinal);
        var subsetNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var subsets = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var subsetOrder = new List<string>();
        var queue = new Queue<string>();
        var transitions = new List<AutomatonTransition>();
        var nextGeneratedName = 0;

        string GetKey(IEnumerable<string> subset)
        {
            var set = new HashSet<string>(subset, StringComparer.Ordinal);
            return string.Join("\u001f", automaton.States.Where(set.Contains));
        }

        string GetSubsetDisplay(IEnumerable<string> subset) => $"{{{string.Join(", ", subset)}}}";

        string AddSubset(IEnumerable<string> subset)
        {
            var orderedSubset = automaton.States.Where(state => subset.Contains(state, StringComparer.Ordinal)).ToArray();
            var key = GetKey(orderedSubset);
            if (subsetNames.TryGetValue(key, out var existingName))
            {
                return existingName;
            }

            string name;
            if (orderedSubset.Length == 1)
            {
                name = orderedSubset[0];
            }
            else
            {
                do
                {
                    name = $"D{nextGeneratedName++}";
                }
                while (originalStates.Contains(name) || usedNames.Contains(name));
            }

            usedNames.Add(name);
            subsetNames.Add(key, name);
            subsets.Add(key, orderedSubset);
            subsetOrder.Add(key);
            queue.Enqueue(key);
            var accepting = orderedSubset.Any(state => automaton.FinalStates.Contains(state, StringComparer.Ordinal));
            AddTrace($"Обнаружено подмножество {GetSubsetDisplay(orderedSubset)} -> {name}{(accepting ? " (конечное)" : string.Empty)}.");
            return name;
        }

        var initialName = AddSubset(new[] { automaton.InitialState });
        AddTrace($"Начальное подмножество: {{{automaton.InitialState}}}.");

        while (queue.Count > 0)
        {
            var sourceKey = queue.Dequeue();
            var sourceSubset = subsets[sourceKey];
            var sourceName = subsetNames[sourceKey];

            foreach (var symbol in automaton.Alphabet)
            {
                var destinationSubset = sourceSubset
                    .SelectMany(state => automaton.GetDestinations(state, symbol))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                var destinationName = AddSubset(destinationSubset);
                transitions.Add(new AutomatonTransition(sourceName, symbol, destinationName));
                AddTrace($"Из {sourceName} {GetSubsetDisplay(sourceSubset)} по '{symbol}' -> {destinationName} {GetSubsetDisplay(subsets[GetKey(destinationSubset)])}.");
            }
        }

        var states = subsetOrder.Select(key => subsetNames[key]).ToArray();
        var finalStates = subsetOrder
            .Where(key => subsets[key].Any(state => automaton.FinalStates.Contains(state, StringComparer.Ordinal)))
            .Select(key => subsetNames[key])
            .ToArray();
        var dfa = new DeterministicFiniteAutomaton(states, automaton.Alphabet, initialName, finalStates, transitions);
        AddTrace($"Детерминизация завершена: состояний ДКА - {dfa.States.Count}, конечных - {dfa.FinalStates.Count}.");
        return new DeterminizationResult(dfa, trace?.ToArray() ?? Array.Empty<string>());
    }
}
