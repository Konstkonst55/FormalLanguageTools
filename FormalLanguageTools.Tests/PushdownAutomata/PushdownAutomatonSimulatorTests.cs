using System;
using System.Linq;
using FormalLanguageTools.PushdownAutomata.Models;
using FormalLanguageTools.PushdownAutomata.Parsing;
using FormalLanguageTools.PushdownAutomata.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FormalLanguageTools.Tests.PushdownAutomata;

[TestClass]
public sealed class PushdownAutomatonSimulatorTests
{
    private const string Example = "InitialStack;Z\n->*q0;0;Z;q0;0Z\nq0;0;0;q0;00\nq0;1;0;q1;e\nq1;1;0;q1;e\nq1;-;Z;q0;e\nq0;-;Z;q0;e";

    private readonly PushdownAutomatonSimulator _simulator = new();
    private readonly DeterministicPushdownAutomaton _automaton = new PushdownAutomatonParser().Parse(Example);

    [DataTestMethod]
    [DataRow("")]
    [DataRow("01")]
    [DataRow("0011")]
    [DataRow("000111")]
    public void Simulate_AcceptsWordsWithEqualNumberOfZerosAndOnes(string input)
    {
        var result = _simulator.Simulate(_automaton, input);

        Assert.AreEqual(ExecutionStatus.Accepted, result.Status);
        Assert.IsTrue(result.IsAccepted);
        Assert.AreEqual(input.Length, result.InputPosition);
        Assert.AreEqual(string.Empty, result.Stack);
    }

    [DataTestMethod]
    [DataRow("0")]
    [DataRow("1")]
    [DataRow("001101")]
    [DataRow("10")]
    [DataRow("011")]
    public void Simulate_RejectsWordsOutsideTheLanguage(string input)
    {
        var result = _simulator.Simulate(_automaton, input);

        Assert.AreEqual(ExecutionStatus.Rejected, result.Status);
        Assert.IsFalse(result.IsAccepted);
    }

    [TestMethod]
    public void Simulate_PrioritizesInputTransitionOverNoInputTransition()
    {
        var result = _simulator.Simulate(_automaton, "0011");

        Assert.AreEqual(ExecutionStatus.Accepted, result.Status);
        Assert.AreEqual<char?>('0', result.Steps[0].ConsumedSymbol);
    }

    [TestMethod]
    public void Simulate_UsesNoInputTransitionWhenNoMatchingInputTransitionExists()
    {
        var result = _simulator.Simulate(_automaton, "001101");

        Assert.IsTrue(result.Steps.Any(step => step.StateBefore == "q1" && step.ConsumedSymbol is null));
        Assert.AreEqual(ExecutionStatus.Rejected, result.Status);
        Assert.AreEqual(string.Empty, result.Stack);
        Assert.IsTrue(result.InputPosition < result.Input.Length);
    }

    [TestMethod]
    public void Simulate_ReportsInputSymbolsOutsideTheAlphabet()
    {
        var result = _simulator.Simulate(_automaton, "00x11");

        Assert.AreEqual(ExecutionStatus.InvalidInput, result.Status);
        Assert.IsTrue(result.Reason.Contains("отсутствует во входном алфавите", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Simulate_ReportsRepeatedNoInputConfigurationAsNonTerminatingLoop()
    {
        const string text = "InitialStack;Z\n->*q0;-;Z;q1;Z\nq1;-;Z;q0;Z";
        var automaton = new PushdownAutomatonParser().Parse(text);

        var result = _simulator.Simulate(automaton, string.Empty);

        Assert.AreEqual(ExecutionStatus.NonTerminatingLoop, result.Status);
        Assert.AreEqual(2, result.Steps.Count);
    }

    [TestMethod]
    public void Simulate_StopsGrowingStackOnNoInputTransitionsAtStepLimit()
    {
        const string text = "InitialStack;Z\n->*q0;-;Z;q0;ZZ";
        var automaton = new PushdownAutomatonParser().Parse(text);

        var result = _simulator.Simulate(automaton, string.Empty, 5);

        Assert.AreEqual(ExecutionStatus.StepLimitExceeded, result.Status);
        Assert.AreEqual(5, result.Steps.Count);
    }
}
