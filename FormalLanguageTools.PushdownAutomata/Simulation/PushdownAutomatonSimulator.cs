using FormalLanguageTools.PushdownAutomata.Models;

namespace FormalLanguageTools.PushdownAutomata.Simulation;

public sealed class PushdownAutomatonSimulator
{
    public const int DefaultMaximumSteps = 100_000;
    public const int MaximumRecordedSteps = 2_000;
    private const int MaximumTrackedStackLength = 256;

    public RecognitionResult Simulate(DeterministicPushdownAutomaton automaton, string input, int maximumSteps = DefaultMaximumSteps)
    {
        ArgumentNullException.ThrowIfNull(automaton);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumSteps);

        var state = automaton.InitialState;
        var inputPosition = 0;
        var stack = automaton.InitialStackSymbol.ToString();
        var steps = new List<ExecutionStep>();
        var totalSteps = 0;
        var isTraceTruncated = false;
        var visitedConfigurations = new HashSet<ConfigurationSignature>();

        while (true)
        {
            if (inputPosition == input.Length && stack.Length == 0)
            {
                var status = automaton.IsFinalState(state) ? ExecutionStatus.Accepted : ExecutionStatus.Rejected;
                var reason = status == ExecutionStatus.Accepted
                    ? "Входная цепочка полностью прочитана, стек пуст, текущее состояние конечное."
                    : "Входная цепочка прочитана и стек пуст, но текущее состояние не является конечным.";
                return CreateResult(input, status, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, reason);
            }

            if (stack.Length == 0)
            {
                return CreateResult(input, ExecutionStatus.Rejected, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, "Стек опустел до завершения чтения входной цепочки.");
            }

            if (stack.Length <= MaximumTrackedStackLength)
            {
                var signature = new ConfigurationSignature(state, inputPosition, stack);
                if (!visitedConfigurations.Add(signature))
                {
                    return CreateResult(input, ExecutionStatus.NonTerminatingLoop, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, "Обнаружено повторение конфигурации. Автомат вошёл в бесконечный цикл переходов без чтения входа.");
                }
            }

            var stackTop = stack[0];
            PushdownTransition? transition = null;
            char? consumedSymbol = null;

            if (inputPosition < input.Length)
            {
                var nextInputSymbol = input[inputPosition];
                if (!automaton.InputAlphabet.Contains(nextInputSymbol))
                {
                    return CreateResult(input, ExecutionStatus.InvalidInput, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, $"Входной символ '{nextInputSymbol}' отсутствует во входном алфавите автомата.");
                }

                if (automaton.TryGetTransition(state, nextInputSymbol, stackTop, out transition))
                {
                    consumedSymbol = nextInputSymbol;
                }
            }

            if (transition is null)
            {
                automaton.TryGetTransition(state, null, stackTop, out transition);
            }

            if (transition is null)
            {
                var remainingInput = inputPosition < input.Length ? input[inputPosition..] : string.Empty;
                var reason = remainingInput.Length > 0
                    ? $"Нет перехода для состояния '{state}', входного символа '{input[inputPosition]}' и вершины стека '{stackTop}'. Подходящий переход без чтения входа ('-') также отсутствует."
                    : $"Входная цепочка прочитана, но нет перехода без чтения входа ('-') для состояния '{state}' и вершины стека '{stackTop}'.";
                return CreateResult(input, ExecutionStatus.Rejected, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, reason);
            }

            if (totalSteps >= maximumSteps)
            {
                return CreateResult(input, ExecutionStatus.StepLimitExceeded, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, $"Превышен установленный предел в {maximumSteps} шагов. Результат распознавания не определён.");
            }

            var stateBefore = state;
            var stackBefore = stack;
            var inputPositionBefore = inputPosition;
            state = transition.NextState;
            stack = transition.Replacement + stack[1..];
            if (consumedSymbol.HasValue)
            {
                inputPosition++;
            }

            totalSteps++;
            if (steps.Count < MaximumRecordedSteps)
            {
                steps.Add(new ExecutionStep(totalSteps, stateBefore, inputPositionBefore, stackBefore, consumedSymbol, state, stack));
            }
            else
            {
                isTraceTruncated = true;
            }
        }
    }

    private static RecognitionResult CreateResult(string input, ExecutionStatus status, string state, int inputPosition, string stack, int totalSteps, IEnumerable<ExecutionStep> steps, bool isTraceTruncated, string reason)
    {
        return new RecognitionResult(input, status, state, inputPosition, stack, totalSteps, steps, isTraceTruncated, reason);
    }

    private readonly record struct ConfigurationSignature(string State, int InputPosition, string Stack);
}
