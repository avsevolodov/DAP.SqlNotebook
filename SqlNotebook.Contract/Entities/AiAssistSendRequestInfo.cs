namespace DAP.SqlNotebook.Contract.Entities;

public class AiAssistSendRequestInfo
{
    public Guid NotebookId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? SqlContext { get; set; }
    public List<string>? Entities { get; set; }
    /// <summary>Current database name for USE [DatabaseName] or qualified names in generated SQL.</summary>
    public string? DatabaseName { get; set; }
    /// <summary>Selected catalog node (source/database) id. When set, AI uses only this source's schema for generation.</summary>
    public Guid? CatalogNodeId { get; set; }
    /// <summary>Predefined skill: "GenerateSql" (default) or "FindTables".</summary>
    public string? Skill { get; set; }
    /// <summary>Chat session id. If null and backend creates a new session, it will be returned in response.</summary>
    public Guid? SessionId { get; set; }
    /// <summary>Previous messages in this chat (user + assistant) to include in generate-sql context. Last N pairs.</summary>
    public List<AiAssistChatTurnInfo>? ChatHistory { get; set; }
}

public class AiAssistChatTurnInfo
{
    public int Role { get; set; }
    public string? Content { get; set; }
}
