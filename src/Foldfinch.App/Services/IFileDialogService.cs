namespace Foldfinch.App.Services;

/// <summary>Abstracts the open/save file pickers so view-models stay independent of the window.</summary>
public interface IFileDialogService
{
    /// <summary>Prompts for one or more PDFs to add; returns their local paths (empty if cancelled).</summary>
    Task<IReadOnlyList<string>> OpenPdfsAsync(string title);

    /// <summary>Prompts for a save location; returns the chosen path, or null if cancelled.</summary>
    Task<string?> SavePdfAsync(string suggestedName);
}
