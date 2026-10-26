using FormalLanguageTools.FiniteAutomata.Exceptions;
using FormalLanguageTools.FiniteAutomata.Models;

namespace FormalLanguageTools.FiniteAutomata.Parsing;

public sealed class TransitionTableParser
{
    private const string EmptyTransitionMarker = "-";
    private const string InitialMarker = "->";
    private const string FinalMarker = "*";

    public NondeterministicFiniteAutomaton Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var nonEmptyLines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n')
            .Select((value, index) => new { Value = value, Line = index + 1 })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToArray();

        if (nonEmptyLines.Length == 0)
        {
            throw new AutomatonFormatException("Таблица переходов пуста.");
        }

        var header = nonEmptyLines[0].Value.Split(';').Select(value => value.Trim()).ToArray();
        header[0] = header[0].TrimStart('\uFEFF');
        if (header.Length < 2 || !string.Equals(header[0], "State", StringComparison.OrdinalIgnoreCase))
        {
            throw new AutomatonFormatException("Первая строка должна иметь формат 'State;символ1;символ2;...'.", nonEmptyLines[0].Line);
        }

        var alphabet = header.Skip(1).ToArray();
        if (alphabet.Any(string.IsNullOrWhiteSpace))
        {
            throw new AutomatonFormatException("Заголовки символов алфавита не могут быть пустыми.", nonEmptyLines[0].Line);
        }

        if (alphabet.Distinct(StringComparer.Ordinal).Count() != alphabet.Length)
        {
            throw new AutomatonFormatException("Символы алфавита должны быть уникальны.", nonEmptyLines[0].Line);
        }

        if (alphabet.Any(IsEpsilonSymbol))
        {
            throw new AutomatonFormatException("ε-переходы в текущей версии не поддерживаются.", nonEmptyLines[0].Line);
        }

        if (alphabet.Any(symbol => symbol == EmptyTransitionMarker || symbol.Contains(',')))
        {
            throw new AutomatonFormatException("Символ '-' зарезервирован для отсутствующего перехода, а запятая — для разделения состояний-получателей.", nonEmptyLines[0].Line);
        }

        var rows = new List<StateRow>();
        var stateNames = new List<string>();
        var knownStates = new HashSet<string>(StringComparer.Ordinal);
        var initialStates = new HashSet<string>(StringComparer.Ordinal);
        var finalStates = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in nonEmptyLines.Skip(1))
        {
            var cells = line.Value.Split(';').Select(value => value.Trim()).ToArray();
            if (cells.Length != header.Length)
            {
                throw new AutomatonFormatException($"Ожидалось {header.Length} колонок, получено {cells.Length}.", line.Line);
            }

            var stateName = cells[0];
            var isInitial = false;
            var isFinal = false;
            var readMarker = true;
            while (readMarker)
            {
                readMarker = false;
                if (stateName.StartsWith(InitialMarker, StringComparison.Ordinal))
                {
                    if (isInitial)
                    {
                        throw new AutomatonFormatException("Маркер начального состояния указан несколько раз.", line.Line);
                    }

                    isInitial = true;
                    stateName = stateName[InitialMarker.Length..].TrimStart();
                    readMarker = true;
                }
                else if (stateName.StartsWith(FinalMarker, StringComparison.Ordinal))
                {
                    if (isFinal)
                    {
                        throw new AutomatonFormatException("Маркер конечного состояния указан несколько раз.", line.Line);
                    }

                    isFinal = true;
                    stateName = stateName[FinalMarker.Length..].TrimStart();
                    readMarker = true;
                }
            }

            if (string.IsNullOrWhiteSpace(stateName) || stateName == EmptyTransitionMarker || stateName.Contains(',') || stateName.Contains(';'))
            {
                throw new AutomatonFormatException("Некорректное имя состояния.", line.Line);
            }

            if (knownStates.Add(stateName))
            {
                stateNames.Add(stateName);
            }

            if (isInitial)
            {
                initialStates.Add(stateName);
            }

            if (isFinal)
            {
                finalStates.Add(stateName);
            }

            rows.Add(new StateRow(stateName, line.Line, cells.Skip(1).ToArray()));
        }

        if (rows.Count == 0)
        {
            throw new AutomatonFormatException("Таблица должна содержать хотя бы одно состояние.");
        }

        if (initialStates.Count != 1)
        {
            throw new AutomatonFormatException(initialStates.Count == 0 ? "Не указано начальное состояние. Используйте маркер '->'." : "Начальное состояние должно быть ровно одно.");
        }

        var transitions = new List<AutomatonTransition>();
        foreach (var row in rows)
        {
            for (var symbolIndex = 0; symbolIndex < alphabet.Length; symbolIndex++)
            {
                var cell = row.TransitionCells[symbolIndex];
                if (cell.Length == 0 || cell == EmptyTransitionMarker)
                {
                    continue;
                }

                var destinations = cell.Split(',').Select(value => value.Trim()).ToArray();
                if (destinations.Any(string.IsNullOrWhiteSpace) || destinations.Any(value => value == EmptyTransitionMarker))
                {
                    throw new AutomatonFormatException($"Некорректный список переходов для символа '{alphabet[symbolIndex]}'. Используйте '-' для отсутствия перехода.", row.LineNumber);
                }

                foreach (var destination in destinations.Distinct(StringComparer.Ordinal))
                {
                    if (!knownStates.Contains(destination))
                    {
                        throw new AutomatonFormatException($"Переход ссылается на неизвестное состояние '{destination}'.", row.LineNumber);
                    }

                    transitions.Add(new AutomatonTransition(row.StateName, alphabet[symbolIndex], destination));
                }
            }
        }

        var stateOrder = stateNames.Select((state, index) => (state, index))
            .ToDictionary(item => item.state, item => item.index, StringComparer.Ordinal);
        var alphabetOrder = alphabet.Select((symbol, index) => (symbol, index))
            .ToDictionary(item => item.symbol, item => item.index, StringComparer.Ordinal);
        var normalizedTransitions = transitions
            .OrderBy(transition => stateOrder[transition.FromState])
            .ThenBy(transition => alphabetOrder[transition.Symbol])
            .ToArray();

        try
        {
            return new NondeterministicFiniteAutomaton(stateNames, alphabet, initialStates.Single(), finalStates, normalizedTransitions);
        }
        catch (ArgumentException exception)
        {
            throw new AutomatonFormatException(exception.Message);
        }
    }

    private static bool IsEpsilonSymbol(string symbol)
    {
        return symbol is "ε" or "λ" || string.Equals(symbol, "epsilon", StringComparison.OrdinalIgnoreCase) || string.Equals(symbol, "lambda", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StateRow(string StateName, int LineNumber, string[] TransitionCells);
}
