using System.Text;
using FormalLanguageTools.ArithmeticParser.Classes;
using FormalLanguageTools.ArithmeticParser.Interfaces;
using FormalLanguageTools.Shared.Exceptions;

namespace FormalLanguageTools.TestBench;

internal static class Program
{
    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write("Введите путь к файлу с выражениями: ");
        var filePath = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Путь к файлу не указан.");
            return 1;
        }

        if (filePath.Length >= 2 && filePath[0] == '"' && filePath[^1] == '"')
        {
            filePath = filePath[1..^1];
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
        {
            Console.Error.WriteLine($"Не удалось прочитать файл: {exception.Message}");
            return 1;
        }

        IArithmeticExpressionParser parser = new ArithmeticExpressionParser();
        var checkedCount = 0;
        var hasErrors = false;

        for (var index = 0; index < lines.Length; index++)
        {
            var expression = lines[index];
            if (string.IsNullOrWhiteSpace(expression))
            {
                continue;
            }

            checkedCount++;
            try
            {
                parser.Parse(expression);
                Console.WriteLine($"Строка {index + 1}: Выражение корректно.");
            }
            catch (FormalLanguageException exception)
            {
                hasErrors = true;
                Console.WriteLine($"Строка {index + 1}, столбец {exception.Position.Column}: Ошибка! {exception.Message}");
            }
        }

        if (checkedCount == 0)
        {
            Console.WriteLine("Файл не содержит непустых выражений для проверки.");
            return 0;
        }

        Console.WriteLine($"Проверено выражений: {checkedCount}.");
        return hasErrors ? 2 : 0;
    }
}
