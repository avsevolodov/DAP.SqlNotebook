using System.ComponentModel.DataAnnotations;

namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Request to create a catalog node (source / database / table).</summary>
public class CatalogNodeCreateInfo
{
    public Guid? ParentId { get; set; }
    /// <summary>0=Folder, 1=Database, 2=Table.</summary>
    public int Type { get; set; } = 1;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? ConnectionInfo { get; set; }
    public string? DatabaseName { get; set; }
    public string? AuthType { get; set; }
    public string? Login { get; set; }
    /// <summary>Only for Basic auth; sent on create/update, never returned.</summary>
    public string? Password { get; set; }
    public string? Owner { get; set; }
    /// <summary>For table/view node: link to entity.</summary>
    public Guid? EntityId { get; set; }
}
