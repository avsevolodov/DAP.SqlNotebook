using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAP.SqlNotebook.BL.DataAccess.Entities;

[Table("AiAssistSessions")]
public class AiAssistSessionEntity
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(256)]
    public string? UserLogin { get; set; }

    [MaxLength(512)]
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Optional: bind this chat session to a notebook.</summary>
    public Guid? NotebookId { get; set; }
}
