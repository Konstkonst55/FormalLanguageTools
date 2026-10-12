using FormalLanguageTools.ArithmeticParser.Classes;
using FormalLanguageTools.ArithmeticParser.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FormalLanguageTools.Tests.ArithmeticParser;

[TestClass]
public sealed class ArithmeticExpressionParserTests
{
    private readonly ArithmeticExpressionParser _parser = new();

    [TestMethod]
    [DataRow("2 + 3 * 4")]
    [DataRow("a * (b - 10)")]
    [DataRow("((12 + value_1) / 3) - 2")]
    [DataRow("5 + 3")]
    [DataRow("\t42\t")]
    [DataRow("переменная_2 + 15")]
    public void Parse_ValidExpression_DoesNotThrow(string expression)
    {
        _parser.Parse(expression);
    }

    [TestMethod]
    public void Parse_EmptyExpression_ThrowsSyntaxExceptionAtFirstColumn()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse(string.Empty));

        Assert.AreEqual(1, exception.Position.Line);
        Assert.AreEqual(1, exception.Position.Column);
        CollectionAssert.AreEqual(new[] { "number", "id", "'('" }, exception.ExpectedTokens.ToArray());
    }

    [TestMethod]
    public void Parse_OperatorWithoutRightOperand_ThrowsSyntaxExceptionAtEndOfInput()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse("5 +"));

        Assert.AreEqual(4, exception.Position.Column);
        Assert.AreEqual("конец ввода", exception.ActualToken);
    }

    [TestMethod]
    public void Parse_UnclosedParenthesis_ExpectsClosingParenthesis()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse("(7 * 2"));

        Assert.AreEqual(7, exception.Position.Column);
        CollectionAssert.AreEqual(new[] { "')'" }, exception.ExpectedTokens.ToArray());
    }

    [TestMethod]
    public void Parse_ExtraClosingParenthesis_ThrowsSyntaxExceptionAtUnexpectedToken()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse("(1 + 2))"));

        Assert.AreEqual(8, exception.Position.Column);
        Assert.AreEqual("')'", exception.ActualToken);
    }

    [TestMethod]
    public void Parse_InvalidCharacter_ThrowsLexicalExceptionWithPosition()
    {
        var exception = Assert.ThrowsExactly<LexicalException>(() => _parser.Parse("2 + @"));

        Assert.AreEqual('@', exception.Character);
        Assert.AreEqual(5, exception.Position.Column);
    }

    [TestMethod]
    public void Parse_LeadingOperator_ThrowsSyntaxException()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse("* 2"));

        Assert.AreEqual(1, exception.Position.Column);
        CollectionAssert.AreEqual(new[] { "number", "id", "'('" }, exception.ExpectedTokens.ToArray());
    }

    [TestMethod]
    public void Parse_MissingOperatorBetweenOperands_ThrowsSyntaxException()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => _parser.Parse("2 3"));

        Assert.AreEqual(3, exception.Position.Column);
        Assert.AreEqual("конец выражения", exception.ExpectedTokens.Single());
    }

    [TestMethod]
    public void Parse_ValidExpression_ReturnsFullParseTreeWithOperatorPrecedence()
    {
        var root = _parser.Parse("2 + 3 * 4");

        Assert.AreEqual("E", root.Name);
        Assert.AreEqual("T", root.Children[0].Name);
        Assert.AreEqual("E'", root.Children[1].Name);

        var expressionPrime = root.Children[1];
        Assert.AreEqual("+", expressionPrime.Children[0].Name);
        var rightTerm = expressionPrime.Children[1];
        Assert.AreEqual("T", rightTerm.Name);
        Assert.AreEqual("*", rightTerm.Children[1].Children[0].Name);
        Assert.AreEqual("4", rightTerm.Children[1].Children[1].Children[0].Value);
        Assert.AreEqual("ε", rightTerm.Children[1].Children[2].Children[0].Name);
        Assert.AreEqual("ε", expressionPrime.Children[2].Children[0].Name);
    }

    [TestMethod]
    public void Parse_ParenthesizedExpression_PreservesParenthesesInTree()
    {
        var root = _parser.Parse("(a + b) * 2");
        var factor = root.Children[0].Children[0];

        Assert.AreEqual("F", factor.Name);
        Assert.AreEqual("(", factor.Children[0].Name);
        Assert.AreEqual("E", factor.Children[1].Name);
        Assert.AreEqual(")", factor.Children[2].Name);
        Assert.AreEqual("id", factor.Children[1].Children[0].Children[0].Children[0].Name);
        Assert.AreEqual("a", factor.Children[1].Children[0].Children[0].Children[0].Value);
    }

    [TestMethod]
    public void Parse_SimpleExpression_ReturnsEpsilonProductions()
    {
        var root = _parser.Parse("42");
        var term = root.Children[0];

        Assert.AreEqual("number", term.Children[0].Children[0].Name);
        Assert.AreEqual("42", term.Children[0].Children[0].Value);
        Assert.AreEqual("ε", root.Children[1].Children[0].Name);
        Assert.AreEqual("ε", term.Children[1].Children[0].Name);
    }
}
