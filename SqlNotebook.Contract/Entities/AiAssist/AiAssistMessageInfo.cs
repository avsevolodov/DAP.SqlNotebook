using System;

namespace DAP.SqlNotebook.Contract.Entities;

public class AiAssistMessageInfo
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public string? Content { get; set; }
    /// <summary>0 = User, 1 = Assistant</summary>
    public int Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
