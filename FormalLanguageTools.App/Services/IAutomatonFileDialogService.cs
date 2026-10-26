namespace FormalLanguageTools.App.Services;

public interface IAutomatonFileDialogService
{
    string? OpenInputFile();

    string? SaveFile(string suggestedFileName);
}
