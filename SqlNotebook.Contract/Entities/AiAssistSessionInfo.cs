namespace DAP.SqlNotebook.Contract.Entities;

public class AiAssistSessionInfo
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? NotebookId { get; set; }
}
