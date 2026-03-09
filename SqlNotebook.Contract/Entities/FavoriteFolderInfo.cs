namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>User-created folder in Favorites section to group favorite notebooks.</summary>
public class FavoriteFolderInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Parent folder id; null = root level in Favorites.</summary>
    public Guid? ParentId { get; set; }
}
