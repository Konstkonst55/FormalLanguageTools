using System;
using FormalLanguageTools.PushdownAutomata.Exceptions;
using FormalLanguageTools.PushdownAutomata.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FormalLanguageTools.Tests.PushdownAutomata;

[TestClass]
public sealed class PushdownAutomatonParserTests
{
    private const string Example = "InitialStack;Z\n->*q0;0;Z;q0;0Z\nq0;0;0;q0;00\nq0;1;0;q1;e\nq1;1;0;q1;e\nq1;-;Z;q0;e\nq0;-;Z;q0;e";

    [TestMethod]
    public void Parse_ReadsInitialStateFinalStateAndTransitions()
    {
        var automaton = new PushdownAutomatonParser().Parse(Example);

        Assert.AreEqual("q0", automaton.InitialState);
        Assert.AreEqual('Z', automaton.InitialStackSymbol);
        Assert.IsTrue(automaton.IsFinalState("q0"));
        Assert.AreEqual(6, automaton.Transitions.Count);
        Assert.IsTrue(automaton.TryGetTransition("q0", '0', 'Z', out var transition));
        Assert.AreEqual("0Z", transition!.Replacement);
    }

    [TestMethod]
    public void Parse_SupportsFinalStateDeclarationWithoutOutgoingTransitions()
    {
        const string text = "InitialStack;Z\nInitialState;q0\nFinalStates;qf\nq0;a;Z;qf;e";

        var automaton = new PushdownAutomatonParser().Parse(text);

        Assert.AreEqual("q0", automaton.InitialState);
        Assert.IsTrue(automaton.IsFinalState("qf"));
    }

    [TestMethod]
    public void Parse_SupportsNoInputTransitionAndEmptyReplacement()
    {
        const string text = "InitialStack;Z\n->*q0;-;Z;q0;e";

        var automaton = new PushdownAutomatonParser().Parse(text);

        Assert.IsTrue(automaton.TryGetTransition("q0", null, 'Z', out var transition));
        Assert.AreEqual(string.Empty, transition!.Replacement);
    }

    [TestMethod]
    public void Parse_RejectsDashAsStackReplacement()
    {
        const string text = "InitialStack;Z\n->*q0;a;Z;q0;-";

        var exception = Assert.ThrowsExactly<PushdownAutomatonFormatException>(() => new PushdownAutomatonParser().Parse(text));

        StringAssert.Contains(exception.Message, "Для удаления вершины стека используйте 'e'");
    }

    [TestMethod]
    public void Parse_ThrowsWhenTransitionKeyIsDuplicated()
    {
        const string text = "InitialStack;Z\n->q0;a;Z;q1;e\nq0;a;Z;q2;e";

        var exception = Assert.ThrowsExactly<PushdownAutomatonFormatException>(() => new PushdownAutomatonParser().Parse(text));

        Assert.AreEqual(3, exception.LineNumber);
    }

    [TestMethod]
    public void Parse_ThrowsWhenInitialStateIsMissing()
    {
        const string text = "InitialStack;Z\nq0;a;Z;q1;e";

        Assert.ThrowsExactly<PushdownAutomatonFormatException>(() => new PushdownAutomatonParser().Parse(text));
    }

    [TestMethod]
    public void Parse_ThrowsWhenRequiredInitialStackIsMissing()
    {
        const string text = "->q0;a;Z;q1;e";

        Assert.ThrowsExactly<PushdownAutomatonFormatException>(() => new PushdownAutomatonParser().Parse(text));
    }
}
