using FormalLanguageTools.PushdownAutomata.Parsing;
using FormalLanguageTools.PushdownAutomata.Simulation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FormalLanguageTools.Tests.PushdownAutomata;

[TestClass]
public sealed class PushdownAutomatonSamplesTests
{
    private readonly PushdownAutomatonParser _parser = new();
    private readonly PushdownAutomatonSimulator _simulator = new();

    [DataTestMethod]
    [DataRow("")]
    [DataRow("01")]
    [DataRow("0011")]
    [DataRow("000111")]
    public void ZeroOneEqualCounts_AcceptsWordsInLanguage(string word)
    {
        var automaton = _parser.Parse(ZeroOneEqualCountsRules);

        Assert.IsTrue(_simulator.Simulate(automaton, word).IsAccepted);
    }

    [DataTestMethod]
    [DataRow("0")]
    [DataRow("1")]
    [DataRow("001")]
    [DataRow("011")]
    public void ZeroOneEqualCounts_RejectsWordsOutsideLanguage(string word)
    {
        var automaton = _parser.Parse(ZeroOneEqualCountsRules);

        Assert.IsFalse(_simulator.Simulate(automaton, word).IsAccepted);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("()")]
    [DataRow("(())")]
    [DataRow("()()")]
    [DataRow("(()())")]
    public void BalancedParentheses_AcceptsCorrectSequences(string word)
    {
        var automaton = _parser.Parse(BalancedParenthesesRules);

        Assert.IsTrue(_simulator.Simulate(automaton, word).IsAccepted);
    }

    [DataTestMethod]
    [DataRow("(")]
    [DataRow(")")]
    [DataRow("(()")]
    [DataRow("())")]
    [DataRow("(()))")]
    public void BalancedParentheses_RejectsIncorrectSequences(string word)
    {
        var automaton = _parser.Parse(BalancedParenthesesRules);

        Assert.IsFalse(_simulator.Simulate(automaton, word).IsAccepted);
    }

    [DataTestMethod]
    [DataRow("#")]
    [DataRow("a#a")]
    [DataRow("b#b")]
    [DataRow("ab#ba")]
    [DataRow("ba#ab")]
    public void MarkedPalindrome_AcceptsWordsInLanguage(string word)
    {
        var automaton = _parser.Parse(MarkedPalindromeRules);

        Assert.IsTrue(_simulator.Simulate(automaton, word).IsAccepted);
    }

    [DataTestMethod]
    [DataRow("a#b")]
    [DataRow("ab#ab")]
    [DataRow("ba#ba")]
    public void MarkedPalindrome_RejectsWordsOutsideLanguage(string word)
    {
        var automaton = _parser.Parse(MarkedPalindromeRules);

        Assert.IsFalse(_simulator.Simulate(automaton, word).IsAccepted);
    }

    private const string ZeroOneEqualCountsRules = "InitialStack;Z\n->*q0;0;Z;q0;0Z\nq0;0;0;q0;00\nq0;1;0;q1;e\nq1;1;0;q1;e\nq1;-;Z;q0;e\nq0;-;Z;q0;e";
    private const string BalancedParenthesesRules = "InitialStack;Z\n->*q0;(;Z;q0;(Z\nq0;(;(;q0;((\nq0;);(;q0;e\nq0;-;Z;q0;e";
    private const string MarkedPalindromeRules = "InitialStack;Z\nInitialState;q0\nFinalStates;qf\nq0;a;Z;q0;aZ\nq0;a;a;q0;aa\nq0;a;b;q0;ab\nq0;b;Z;q0;bZ\nq0;b;a;q0;ba\nq0;b;b;q0;bb\nq0;#;Z;q1;Z\nq0;#;a;q1;a\nq0;#;b;q1;b\nq1;a;a;q1;e\nq1;b;b;q1;e\nq1;-;Z;qf;e";
}
