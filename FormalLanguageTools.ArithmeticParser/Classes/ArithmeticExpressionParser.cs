using FormalLanguageTools.ArithmeticParser.Interfaces;

namespace FormalLanguageTools.ArithmeticParser.Classes;

public sealed class ArithmeticExpressionParser : IArithmeticExpressionParser
{
    public void Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var tokens = Lexer.Tokenize(expression);
        var parser = new RecursiveDescentParser(tokens);
        parser.Parse();
    }
}
