using FormalLanguageTools.ArithmeticParser.DataClasses;

namespace FormalLanguageTools.App.Models;

public sealed record ExpressionDiagnostic(
    int LineNumber,
    int Column,
    string Expression,
    string Status,
    string Message,
    bool IsError,
    SyntaxNode? SyntaxTree = null);
