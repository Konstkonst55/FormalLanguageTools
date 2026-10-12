using FormalLanguageTools.ArithmeticParser.DataClasses;

namespace FormalLanguageTools.ArithmeticParser.Interfaces;

public interface IArithmeticExpressionParser
{
    SyntaxNode Parse(string expression);
}
