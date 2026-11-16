using FormalLanguageTools.PushdownAutomata.Exceptions;
using FormalLanguageTools.PushdownAutomata.Models;

namespace FormalLanguageTools.PushdownAutomata.Parsing;

public sealed class PushdownAutomatonParser
{
    private const string InitialStackHeader = "InitialStack";
    private const string InitialStateHeader = "InitialState";
    private const string FinalStatesHeader = "FinalStates";
    private const string TransitionHeader = "State;Input;StackTop;NextState;ReplaceTop";

    public DeterministicPushdownAutomaton Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        char? initialStackSymbol = null;
        string? initialState = null;
        var finalStates = new HashSet<string>(StringComparer.Ordinal);
        var transitions = new List<PushdownTransition>();
        var transitionKeys = new HashSet<PushdownTransitionKey>();

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.Equals(TransitionHeader, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fields = line.Split(';');
            if (fields.Length == 2 && fields[0].Trim().Equals(InitialStackHeader, StringComparison.OrdinalIgnoreCase))
            {
                if (initialStackSymbol.HasValue)
                {
                    throw new PushdownAutomatonFormatException("Начальный символ стека задан несколько раз.", lineNumber);
                }

                initialStackSymbol = ParseSingleSymbol(fields[1], "Начальный символ стека", lineNumber);
                continue;
            }

            if (fields.Length == 2 && fields[0].Trim().Equals(InitialStateHeader, StringComparison.OrdinalIgnoreCase))
            {
                if (initialState is not null)
                {
                    throw new PushdownAutomatonFormatException("Начальное состояние задано несколько раз.", lineNumber);
                }

                initialState = ParseStateName(fields[1], "Начальное состояние", lineNumber);
                continue;
            }

            if (fields.Length == 2 && fields[0].Trim().Equals(FinalStatesHeader, StringComparison.OrdinalIgnoreCase))
            {
                var declaredStates = fields[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (declaredStates.Length == 0)
                {
                    throw new PushdownAutomatonFormatException("Список конечных состояний не может быть пустым.", lineNumber);
                }

                foreach (var declaredState in declaredStates)
                {
                    finalStates.Add(ParseStateName(declaredState, "Конечное состояние", lineNumber));
                }

                continue;
            }

            if (fields.Length != 5)
            {
                throw new PushdownAutomatonFormatException("Ожидается пять полей: Состояние;Вход;ВершинаСтекa;СледующееСостояние;ЗаменаВершины.", lineNumber);
            }

            var (currentState, isInitial, isFinal) = ParseMarkedState(fields[0], lineNumber);
            if (isInitial)
            {
                if (initialState is not null && !initialState.Equals(currentState, StringComparison.Ordinal))
                {
                    throw new PushdownAutomatonFormatException("Обнаружено более одного начального состояния.", lineNumber);
                }

                initialState = currentState;
            }

            if (isFinal)
            {
                finalStates.Add(currentState);
            }

            var inputSymbol = ParseOptionalSymbol(fields[1], "Входной символ", lineNumber);
            var stackTop = ParseSingleSymbol(fields[2], "Символ на вершине стека", lineNumber);
            var nextState = ParseStateName(fields[3], "Следующее состояние", lineNumber);
            var replacement = ParseReplacement(fields[4], lineNumber);
            var transition = new PushdownTransition(currentState, inputSymbol, stackTop, nextState, replacement);
            var key = new PushdownTransitionKey(currentState, inputSymbol, stackTop);

            if (!transitionKeys.Add(key))
            {
                throw new PushdownAutomatonFormatException($"Переход для состояния '{currentState}', входного символа '{FormatInputSymbol(inputSymbol)}' и вершины стека '{stackTop}' задан более одного раза.", lineNumber);
            }

            transitions.Add(transition);
        }

        if (!initialStackSymbol.HasValue)
        {
            throw new PushdownAutomatonFormatException($"Не задан обязательный параметр {InitialStackHeader};<символ>.");
        }

        if (initialState is null)
        {
            throw new PushdownAutomatonFormatException("Не задано начальное состояние. Используйте маркер -> перед состоянием или строку InitialState;имя.");
        }

        if (transitions.Count == 0)
        {
            throw new PushdownAutomatonFormatException("Не задано ни одного перехода автомата.");
        }

        return new DeterministicPushdownAutomaton(initialState, initialStackSymbol.Value, finalStates, transitions);
    }

    private static (string State, bool IsInitial, bool IsFinal) ParseMarkedState(string value, int lineNumber)
    {
        var state = value.Trim();
        var isInitial = false;
        var isFinal = false;
        var markerFound = true;

        while (markerFound)
        {
            markerFound = false;
            if (state.StartsWith("->", StringComparison.Ordinal))
            {
                if (isInitial)
                {
                    throw new PushdownAutomatonFormatException("Маркер начального состояния указан несколько раз.", lineNumber);
                }

                isInitial = true;
                state = state[2..].Trim();
                markerFound = true;
            }
            else if (state.StartsWith('*'))
            {
                if (isFinal)
                {
                    throw new PushdownAutomatonFormatException("Маркер конечного состояния указан несколько раз.", lineNumber);
                }

                isFinal = true;
                state = state[1..].Trim();
                markerFound = true;
            }
        }

        if (state.Length == 0 || state.Contains("->", StringComparison.Ordinal) || state.Contains('*'))
        {
            throw new PushdownAutomatonFormatException("Некорректное имя состояния или маркеры состояния.", lineNumber);
        }

        return (state, isInitial, isFinal);
    }

    private static string ParseStateName(string value, string fieldName, int lineNumber)
    {
        var state = value.Trim();
        if (state.Length == 0 || state.Contains(';') || state.Contains('*') || state.Contains("->", StringComparison.Ordinal))
        {
            throw new PushdownAutomatonFormatException($"Поле '{fieldName}' содержит некорректное имя состояния.", lineNumber);
        }

        return state;
    }

    private static char ParseSingleSymbol(string value, string fieldName, int lineNumber)
    {
        var symbol = value.Trim();
        if (symbol.Length != 1 || char.IsWhiteSpace(symbol[0]) || symbol[0] == ';')
        {
            throw new PushdownAutomatonFormatException($"Поле '{fieldName}' должно содержать ровно один непробельный символ.", lineNumber);
        }

        return symbol[0];
    }

    private static char? ParseOptionalSymbol(string value, string fieldName, int lineNumber)
    {
        var symbol = value.Trim();
        if (symbol == "-")
        {
            return null;
        }

        return ParseSingleSymbol(symbol, fieldName, lineNumber);
    }

    private static string ParseReplacement(string value, int lineNumber)
    {
        var replacement = value.Trim();
        if (replacement == "e")
        {
            return string.Empty;
        }

        if (replacement == "-")
        {
            throw new PushdownAutomatonFormatException("Символ '-' обозначает переход без чтения входа. Для удаления вершины стека используйте 'e'.", lineNumber);
        }

        if (replacement.Length == 0 || replacement.Contains(';') || replacement.Any(char.IsWhiteSpace))
        {
            throw new PushdownAutomatonFormatException("Замена вершины должна быть последовательностью стековых символов или 'e' для удаления вершины.", lineNumber);
        }

        return replacement;
    }

    private static string FormatInputSymbol(char? inputSymbol)
    {
        return inputSymbol?.ToString() ?? "-";
    }
}
