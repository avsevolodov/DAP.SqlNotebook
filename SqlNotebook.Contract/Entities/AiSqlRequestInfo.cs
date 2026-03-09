namespace DAP.SqlNotebook.Contract.Entities;

public class AiSqlRequestInfo
{
    public string Prompt { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public List<string>? Entities { get; set; }
    public string? SqlContext { get; set; }
}
