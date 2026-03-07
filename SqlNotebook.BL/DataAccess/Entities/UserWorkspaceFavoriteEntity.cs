using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities;

[Table("UserWorkspaceFavorites")]
public class UserWorkspaceFavoriteEntity
{
    [Required]
    [MaxLength(256)]
    public string UserLogin { get; set; } = string.Empty;

    public Guid WorkspaceId { get; set; }
}
