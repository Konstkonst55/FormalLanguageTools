using FormalLanguageTools.ArithmeticParser.Enums;
using FormalLanguageTools.Shared.DataClasses;

namespace FormalLanguageTools.ArithmeticParser.DataClasses;

internal sealed record Token(TokenType Type, string Value, SourcePosition Position);
