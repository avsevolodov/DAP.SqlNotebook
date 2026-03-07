using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities;

[Table("UserNotebookFavorites")]
public class UserNotebookFavoriteEntity
{
    [Required]
    [MaxLength(256)]
    public string UserLogin { get; set; } = string.Empty;

    public Guid NotebookId { get; set; }

    /// <summary>Folder in Favorites; null = root level.</summary>
    public Guid? FolderId { get; set; }
}
