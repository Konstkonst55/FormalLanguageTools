using FormalLanguageTools.FiniteAutomata.Algorithms;
using FormalLanguageTools.FiniteAutomata.Exceptions;
using FormalLanguageTools.FiniteAutomata.Formatting;
using FormalLanguageTools.FiniteAutomata.Models;
using FormalLanguageTools.FiniteAutomata.Parsing;
using FormalLanguageTools.FiniteAutomata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FormalLanguageTools.Tests.FiniteAutomata;

[TestClass]
public sealed class AutomatonMinimizationTests
{
    private const string LectureExample = "State;0;1\n->q0;q1;q2\nq1;q4;q2\nq2;q3;q0\nq3;q5;q2\n*q4;q5;q5\n*q5;q4;q4";
    private const string NondeterministicExample = "State;0;1\n->q0;q0,q1;q0\nq1;-;q2\n*q2;-;-";
    private const string RepeatedRowsExample1 = "State;a;b;c\n->q0;q0;-;-\nq0;q1;-;-\nq1;q2;q2;-\nq2;q3;-;q1\n*q3;q3;q3;q3\n*q3;-;-;q2";
    private const string RepeatedRowsExample2 = "State;0;1\n->q0;q0;q0\nq0;q1;q1\nq1;-;q1\nq1;-;q2\n*q2;q2;q2\n*q2;-;q1";
    private const string RepeatedRowsExample3 = "State;a;b;c\n->q0;q0;q0;q0\nq0;q1;-;q1\nq1;-;-;q1\nq1;-;q2;q2\n*q2;q1;q2;q2";
    private const string RepeatedRowsExample4 = "State;a;b\n->q0;q1;-\nq0;q3;-\nq1;q1;q1\nq1;q2;q2\n*q2;q2;q2\nq3;-;q3\nq3;-;q1";
    private readonly TransitionTableParser _parser = new();
    private readonly TransitionTableFormatter _formatter = new();
    private readonly AutomatonMinimizationService _service = new();

    private void AssertCanMinimize(NondeterministicFiniteAutomaton automaton)
    {
        var result = _service.Process(automaton);
        Assert.IsTrue(result.DeterministicAutomaton.States.Count > 0);
        Assert.IsTrue(result.MinimizedAutomaton.States.Count > 0);
    }

    [TestMethod]
    public void Process_LectureExample_MinimizesSixStatesToExpectedThreeStates()
    {
        var nfa = _parser.Parse(LectureExample);
        var result = _service.Process(nfa);
        var minimized = result.MinimizedAutomaton;

        Assert.AreEqual(6, result.DeterministicAutomaton.States.Count);
        Assert.AreEqual(3, minimized.States.Count);
        CollectionAssert.AreEqual(new[] { "Q0", "Q1", "Q4" }, minimized.States.ToArray());
        Assert.AreEqual("Q0", minimized.InitialState);
        CollectionAssert.AreEqual(new[] { "Q4" }, minimized.FinalStates.ToArray());
        Assert.AreEqual("Q1", minimized.GetDestination("Q0", "0"));
        Assert.AreEqual("Q0", minimized.GetDestination("Q0", "1"));
        Assert.AreEqual("Q4", minimized.GetDestination("Q1", "0"));
        Assert.AreEqual("Q0", minimized.GetDestination("Q1", "1"));
        Assert.AreEqual("Q4", minimized.GetDestination("Q4", "0"));
        Assert.AreEqual("Q4", minimized.GetDestination("Q4", "1"));
    }

    [TestMethod]
    public void Process_NondeterministicAutomaton_BuildsDfaUsingSubsetConstruction()
    {
        var nfa = _parser.Parse(NondeterministicExample);
        var result = _service.Process(nfa);

        Assert.AreEqual(3, result.DeterministicAutomaton.States.Count);
        Assert.AreEqual(3, result.MinimizedAutomaton.States.Count);
        Assert.AreEqual(6, result.DeterministicAutomaton.Transitions.Count);
        Assert.IsTrue(result.DeterministicAutomaton.FinalStates.Contains("D1"));
        Assert.AreEqual("D0", result.DeterministicAutomaton.GetDestination("q0", "0"));
        Assert.AreEqual("D1", result.DeterministicAutomaton.GetDestination("D0", "1"));
    }

    [TestMethod]
    public void Process_TraceIsDisabledByDefault_ReturnsNoTraceEntries()
    {
        var result = _service.Process(_parser.Parse(LectureExample));

        Assert.AreEqual(0, result.Trace.Count);
    }

    [TestMethod]
    public void Process_TraceEnabled_ContainsDeterminizationAndMinimizationStages()
    {
        var result = _service.Process(_parser.Parse(LectureExample), true);
        var trace = string.Join(Environment.NewLine, result.Trace);

        StringAssert.Contains(trace, "Детерминизация НКА методом построения подмножеств");
        StringAssert.Contains(trace, "Начальное разбиение P0");
        StringAssert.Contains(trace, "Итерация");
        StringAssert.Contains(trace, "Q0 = {q0, q2}");
        StringAssert.Contains(trace, "Q1 = {q1, q3}");
        StringAssert.Contains(trace, "Q4 = {q4, q5}");
    }

    [TestMethod]
    public void Process_IncompleteNfa_AddsEmptySubsetAndReturnsCompleteDfa()
    {
        var input = "State;0;1\n->*q0;q0;-";
        var result = _service.Process(_parser.Parse(input), true);
        var deterministic = result.DeterministicAutomaton;

        Assert.AreEqual(deterministic.States.Count * deterministic.Alphabet.Count, deterministic.Transitions.Count);
        Assert.AreEqual(2, deterministic.States.Count);
        Assert.IsTrue(deterministic.States.Any(state => state.StartsWith("D", StringComparison.Ordinal)));
        StringAssert.Contains(string.Join(Environment.NewLine, result.Trace), "Обнаружено подмножество {}");
    }

    [TestMethod]
    public void Minimize_IncompleteDfa_AddsSinkStateAndCompletesTransitions()
    {
        var dfa = new DeterministicFiniteAutomaton(
            new[] { "q0" },
            new[] { "0", "1" },
            "q0",
            new[] { "q0" },
            new[] { new AutomatonTransition("q0", "0", "q0") });

        var result = new DfaMinimizer().Minimize(dfa, true);

        Assert.AreEqual(result.Automaton.States.Count * result.Automaton.Alphabet.Count, result.Automaton.Transitions.Count);
        Assert.AreEqual(2, result.Automaton.States.Count);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Trace), "поглощающее состояние");
    }

    [TestMethod]
    public void Process_UnreachableStates_RemovesThemBeforeMinimization()
    {
        var automaton = new DeterministicFiniteAutomaton(
            new[] { "q0", "q1", "q2", "q3" },
            new[] { "0", "1" },
            "q0",
            new[] { "q2" },
            new[]
            {
                new AutomatonTransition("q0", "0", "q1"),
                new AutomatonTransition("q0", "1", "q0"),
                new AutomatonTransition("q1", "0", "q1"),
                new AutomatonTransition("q1", "1", "q0"),
                new AutomatonTransition("q2", "0", "q2"),
                new AutomatonTransition("q2", "1", "q2"),
                new AutomatonTransition("q3", "0", "q3"),
                new AutomatonTransition("q3", "1", "q3")
            });
        var result = new DfaMinimizer().Minimize(automaton, true);

        Assert.AreEqual(1, result.Automaton.States.Count);
        Assert.AreEqual(0, result.Automaton.FinalStates.Count);
        StringAssert.Contains(string.Join(Environment.NewLine, result.Trace), "Удалены недостижимые состояния: {q2, q3}");
    }

    [TestMethod]
    public void Parse_RepeatedRowsExample1_MergesTransitionsAndStateMarkers()
    {
        var automaton = _parser.Parse(RepeatedRowsExample1);

        CollectionAssert.AreEqual(new[] { "q0", "q1", "q2", "q3" }, automaton.States.ToArray());
        Assert.AreEqual("q0", automaton.InitialState);
        CollectionAssert.AreEqual(new[] { "q3" }, automaton.FinalStates.ToArray());
        CollectionAssert.AreEqual(new[] { "q0", "q1" }, automaton.GetDestinations("q0", "a").ToArray());
        CollectionAssert.AreEqual(new[] { "q3", "q2" }, automaton.GetDestinations("q3", "c").ToArray());
        AssertCanMinimize(automaton);
    }

    [TestMethod]
    public void Parse_RepeatedRowsExample2_MergesTransitionsAndStateMarkers()
    {
        var automaton = _parser.Parse(RepeatedRowsExample2);

        CollectionAssert.AreEqual(new[] { "q0", "q1", "q2" }, automaton.States.ToArray());
        CollectionAssert.AreEqual(new[] { "q0", "q1" }, automaton.GetDestinations("q0", "0").ToArray());
        CollectionAssert.AreEqual(new[] { "q1", "q2" }, automaton.GetDestinations("q1", "1").ToArray());
        CollectionAssert.AreEqual(new[] { "q2", "q1" }, automaton.GetDestinations("q2", "1").ToArray());
        CollectionAssert.AreEqual(new[] { "q2" }, automaton.FinalStates.ToArray());
        AssertCanMinimize(automaton);
    }

    [TestMethod]
    public void Parse_RepeatedRowsExample3_MergesTransitionsAndStateMarkers()
    {
        var automaton = _parser.Parse(RepeatedRowsExample3);

        CollectionAssert.AreEqual(new[] { "q0", "q1" }, automaton.GetDestinations("q0", "a").ToArray());
        CollectionAssert.AreEqual(new[] { "q1", "q2" }, automaton.GetDestinations("q1", "c").ToArray());
        CollectionAssert.AreEqual(new[] { "q2" }, automaton.GetDestinations("q1", "b").ToArray());
        CollectionAssert.AreEqual(new[] { "q2" }, automaton.FinalStates.ToArray());
        AssertCanMinimize(automaton);
    }

    [TestMethod]
    public void Parse_RepeatedRowsExample4_MergesTransitionsAndStateMarkers()
    {
        var automaton = _parser.Parse(RepeatedRowsExample4);

        CollectionAssert.AreEqual(new[] { "q1", "q3" }, automaton.GetDestinations("q0", "a").ToArray());
        CollectionAssert.AreEqual(new[] { "q1", "q2" }, automaton.GetDestinations("q1", "a").ToArray());
        CollectionAssert.AreEqual(new[] { "q1", "q2" }, automaton.GetDestinations("q1", "b").ToArray());
        CollectionAssert.AreEqual(new[] { "q3", "q1" }, automaton.GetDestinations("q3", "b").ToArray());
        AssertCanMinimize(automaton);
    }

    [TestMethod]
    public void FormatAndParse_RepeatedStateRows_NormalizesToOneRowPerState()
    {
        var automaton = _parser.Parse(RepeatedRowsExample2);
        var formatted = _formatter.Format(automaton);
        var restored = _parser.Parse(formatted);

        Assert.AreEqual(automaton.States.Count, formatted.Split('\n').Length - 1);
        CollectionAssert.AreEqual(automaton.Transitions.ToArray(), restored.Transitions.ToArray());
        CollectionAssert.AreEqual(automaton.States.ToArray(), restored.States.ToArray());
    }

    [TestMethod]
    public void Parse_DuplicateDestinationsAcrossRows_StoresUniqueTransitions()
    {
        var automaton = _parser.Parse("State;0;1\n->q0;q1;-\nq0;q1;q1\nq1;-;-");

        CollectionAssert.AreEqual(new[] { "q1" }, automaton.GetDestinations("q0", "0").ToArray());
        Assert.AreEqual(2, automaton.Transitions.Count);
    }

    [TestMethod]
    public void Parse_EpsilonAlphabetSymbol_ThrowsFormatException()
    {
        var exception = Assert.ThrowsExactly<AutomatonFormatException>(() => _parser.Parse("State;0;ε\n->q0;q0;q0"));

        StringAssert.Contains(exception.Message, "ε-переходы");
    }

    [TestMethod]
    public void Parse_ReservedEmptyTransitionSymbol_ThrowsFormatException()
    {
        var exception = Assert.ThrowsExactly<AutomatonFormatException>(() => _parser.Parse("State;-;1\n->q0;q0;q0"));

        StringAssert.Contains(exception.Message, "зарезервирован");
    }

    [TestMethod]
    public void Parse_UnknownDestination_ThrowsFormatExceptionWithLineNumber()
    {
        var exception = Assert.ThrowsExactly<AutomatonFormatException>(() => _parser.Parse("State;0;1\n->q0;q1;-"));

        Assert.AreEqual(2, exception.LineNumber);
        StringAssert.Contains(exception.Message, "неизвестное состояние 'q1'");
    }

    [TestMethod]
    public void FormatAndParse_LectureTable_PreserveTransitions()
    {
        var original = _parser.Parse(LectureExample);
        var formatted = _formatter.Format(original);
        var restored = _parser.Parse(formatted);

        CollectionAssert.AreEqual(original.States.ToArray(), restored.States.ToArray());
        CollectionAssert.AreEqual(original.Alphabet.ToArray(), restored.Alphabet.ToArray());
        CollectionAssert.AreEqual(original.FinalStates.ToArray(), restored.FinalStates.ToArray());
        CollectionAssert.AreEqual(original.Transitions.ToArray(), restored.Transitions.ToArray());
        Assert.AreEqual(original.InitialState, restored.InitialState);
    }
}
