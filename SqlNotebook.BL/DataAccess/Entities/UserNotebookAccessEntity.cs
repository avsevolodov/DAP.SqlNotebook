using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities;

public enum NotebookAccessRoleEntity
{
    Viewer = 0,
    Editor = 1,
}

[Table("UserNotebookAccess")]
public class UserNotebookAccessEntity
{
    [Required]
    [MaxLength(256)]
    public string UserLogin { get; set; } = string.Empty;

    public Guid NotebookId { get; set; }

    public NotebookAccessRoleEntity Role { get; set; } = NotebookAccessRoleEntity.Viewer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

