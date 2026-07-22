namespace Foldfinch.App.Services;

/// <summary>Abstracts the open/save file pickers so view-models stay independent of the window.</summary>
public interface IFileDialogService
{
    /// <summary>Prompts for a single PDF to open; returns its local path, or null if cancelled.</summary>
    Task<string?> OpenPdfAsync(string title);

    /// <summary>Prompts for a save location; returns the chosen path, or null if cancelled.</summary>
    Task<string?> SavePdfAsync(string suggestedName);
}
