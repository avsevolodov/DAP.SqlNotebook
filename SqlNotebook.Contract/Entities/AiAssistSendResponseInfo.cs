namespace DAP.SqlNotebook.Contract.Entities;

public class AiAssistSendResponseInfo
{
    public AiAssistMessageInfo Message { get; set; } = null!;
    public string Sql { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    /// <summary>Current chat session id (set when a new session was created for this send).</summary>
    public Guid? SessionId { get; set; }
}
