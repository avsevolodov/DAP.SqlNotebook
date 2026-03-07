namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>A favorite notebook entry with optional folder.</summary>
public class FavoriteNotebookItemInfo
{
    public Guid NotebookId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    /// <summary>Folder in Favorites this notebook is placed in; null = root of Favorites.</summary>
    public Guid? FolderId { get; set; }
}
