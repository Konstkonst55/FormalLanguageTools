using System.Text.RegularExpressions;
using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Results;

namespace FormalLanguageTools.FiniteAutomata.Algorithms;

public sealed class DfaMinimizer
{
    private static readonly Regex NumberedQStatePattern = new("^[qQ](\\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public MinimizationResult Minimize(DeterministicFiniteAutomaton automaton, bool captureTrace = false)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        var trace = captureTrace ? new List<string>() : null;
        void AddTrace(string message) => trace?.Add(message);

        AddTrace("Этап 2. Минимизация ДКА методом уточнения классов эквивалентности.");
        var reachableStates = FindReachableStates(automaton);
        var unreachableStates = automaton.States.Where(state => !reachableStates.Contains(state)).ToArray();
        if (unreachableStates.Length > 0)
        {
            AddTrace($"Удалены недостижимые состояния: {FormatSet(unreachableStates)}.");
        }
        else
        {
            AddTrace("Все состояния исходного ДКА достижимы.");
        }

        var states = automaton.States.Where(reachableStates.Contains).ToList();
        var transitions = automaton.Transitions
            .Where(transition => reachableStates.Contains(transition.FromState) && reachableStates.Contains(transition.ToState))
            .ToList();
        var finalStates = automaton.FinalStates.Where(reachableStates.Contains).ToList();
        var missingTransitions = states
            .SelectMany(state => automaton.Alphabet.Where(symbol => !transitions.Any(transition => transition.FromState == state && transition.Symbol == symbol)).Select(symbol => (State: state, Symbol: symbol)))
            .ToArray();

        if (missingTransitions.Length > 0)
        {
            var sinkState = CreateUniqueStateName("sink", states);
            states.Add(sinkState);
            foreach (var missing in missingTransitions)
            {
                transitions.Add(new AutomatonTransition(missing.State, missing.Symbol, sinkState));
                AddTrace($"Отсутствующий переход из {missing.State} по '{missing.Symbol}' направлен в дополнительное поглощающее состояние {sinkState}.");
            }

            transitions.AddRange(automaton.Alphabet.Select(symbol => new AutomatonTransition(sinkState, symbol, sinkState)));
            AddTrace($"Состояние {sinkState} зациклено по всем символам алфавита для завершения функции переходов.");
        }

        var completeDfa = new DeterministicFiniteAutomaton(states, automaton.Alphabet, automaton.InitialState, finalStates, transitions);
        var partitions = CreateInitialPartition(completeDfa);
        AddTrace($"Начальное разбиение P0: {FormatPartition(partitions)}.");

        var iteration = 0;
        while (true)
        {
            iteration++;
            var blockIndexes = GetBlockIndexes(partitions);
            var refinedPartitions = new List<List<string>>();
            var wasSplit = false;
            AddTrace($"Итерация {iteration}.");

            foreach (var block in partitions)
            {
                var groupsBySignature = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                var signatureDescriptions = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var state in block)
                {
                    var signatureParts = new List<string>();
                    foreach (var symbol in completeDfa.Alphabet)
                    {
                        var destination = completeDfa.GetDestination(state, symbol) ?? throw new InvalidOperationException("Полная таблица переходов ДКА содержит пропуск.");
                        signatureParts.Add(blockIndexes[destination].ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    var signature = string.Join(",", signatureParts);
                    if (!groupsBySignature.TryGetValue(signature, out var group))
                    {
                        group = new List<string>();
                        groupsBySignature.Add(signature, group);
                        signatureDescriptions.Add(signature, $"[{string.Join(", ", signatureParts)}]");
                    }

                    group.Add(state);
                }

                var groups = groupsBySignature.Values.ToList();
                refinedPartitions.AddRange(groups);
                if (groups.Count == 1)
                {
                    AddTrace($"Класс {FormatSet(block)} не разбивается (сигнатура {signatureDescriptions.First().Value}).");
                }
                else
                {
                    wasSplit = true;
                    var groupDescriptions = groupsBySignature.Select(pair => $"{FormatSet(pair.Value)} - {signatureDescriptions[pair.Key]}");
                    AddTrace($"Класс {FormatSet(block)} разбит: {string.Join("; ", groupDescriptions)}.");
                }
            }

            partitions = refinedPartitions;
            AddTrace($"Разбиение после итерации {iteration}: {FormatPartition(partitions)}.");
            if (!wasSplit)
            {
                AddTrace("Разбиение стабилизировалось.");
                break;
            }
        }

        var minimizedStateNames = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < partitions.Count; index++)
        {
            var representative = partitions[index][0];
            var match = NumberedQStatePattern.Match(representative);
            var suggestedName = match.Success ? $"Q{match.Groups[1].Value}" : $"M{index}";
            if (!usedNames.Add(suggestedName))
            {
                suggestedName = CreateUniqueStateName($"M{index}", usedNames);
                usedNames.Add(suggestedName);
            }

            minimizedStateNames.Add(suggestedName);
        }

        var minimizedBlockByState = GetBlockIndexes(partitions);
        var minimizedTransitions = new List<AutomatonTransition>();
        for (var blockIndex = 0; blockIndex < partitions.Count; blockIndex++)
        {
            var representative = partitions[blockIndex][0];
            foreach (var symbol in completeDfa.Alphabet)
            {
                var target = completeDfa.GetDestination(representative, symbol) ?? throw new InvalidOperationException("Полная таблица переходов ДКА содержит пропуск.");
                minimizedTransitions.Add(new AutomatonTransition(minimizedStateNames[blockIndex], symbol, minimizedStateNames[minimizedBlockByState[target]]));
            }
        }

        var initialBlock = minimizedBlockByState[completeDfa.InitialState];
        var minimizedFinalStates = partitions
            .Select((block, index) => (Block: block, Index: index))
            .Where(item => item.Block.Any(state => completeDfa.FinalStates.Contains(state, StringComparer.Ordinal)))
            .Select(item => minimizedStateNames[item.Index])
            .ToArray();
        var minimizedDfa = new DeterministicFiniteAutomaton(minimizedStateNames, completeDfa.Alphabet, minimizedStateNames[initialBlock], minimizedFinalStates, minimizedTransitions);

        AddTrace("Итоговые классы эквивалентности:");
        for (var index = 0; index < partitions.Count; index++)
        {
            var finalMark = partitions[index].Any(state => completeDfa.FinalStates.Contains(state, StringComparer.Ordinal)) ? " (конечное)" : string.Empty;
            var initialMark = partitions[index].Contains(completeDfa.InitialState, StringComparer.Ordinal) ? " (начальное)" : string.Empty;
            AddTrace($"{minimizedStateNames[index]} = {FormatSet(partitions[index])}{initialMark}{finalMark}.");
        }

        AddTrace($"Минимизация завершена: {automaton.States.Count} исходных состояний, {minimizedDfa.States.Count} состояний после минимизации.");
        return new MinimizationResult(minimizedDfa, trace?.ToArray() ?? Array.Empty<string>());
    }

    private static HashSet<string> FindReachableStates(DeterministicFiniteAutomaton automaton)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal) { automaton.InitialState };
        var queue = new Queue<string>();
        queue.Enqueue(automaton.InitialState);
        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            foreach (var symbol in automaton.Alphabet)
            {
                var destination = automaton.GetDestination(state, symbol);
                if (destination is not null && reachable.Add(destination))
                {
                    queue.Enqueue(destination);
                }
            }
        }

        return reachable;
    }

    private static List<List<string>> CreateInitialPartition(DeterministicFiniteAutomaton automaton)
    {
        var finals = automaton.States.Where(state => automaton.FinalStates.Contains(state, StringComparer.Ordinal)).ToList();
        var nonFinals = automaton.States.Where(state => !automaton.FinalStates.Contains(state, StringComparer.Ordinal)).ToList();
        var partitions = new List<List<string>>();
        if (nonFinals.Count > 0)
        {
            partitions.Add(nonFinals);
        }

        if (finals.Count > 0)
        {
            partitions.Add(finals);
        }

        return partitions;
    }

    private static Dictionary<string, int> GetBlockIndexes(IReadOnlyList<List<string>> partitions)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < partitions.Count; index++)
        {
            foreach (var state in partitions[index])
            {
                result[state] = index;
            }
        }

        return result;
    }

    private static string FormatSet(IEnumerable<string> states) => $"{{{string.Join(", ", states)}}}";

    private static string FormatPartition(IEnumerable<List<string>> partitions) => $"{{{string.Join(", ", partitions.Select(FormatSet))}}}";

    private static string CreateUniqueStateName(string baseName, IEnumerable<string> existingStates)
    {
        var existing = new HashSet<string>(existingStates, StringComparer.Ordinal);
        var candidate = baseName;
        var suffix = 1;
        while (existing.Contains(candidate))
        {
            candidate = $"{baseName}_{suffix++}";
        }

        return candidate;
    }
}
