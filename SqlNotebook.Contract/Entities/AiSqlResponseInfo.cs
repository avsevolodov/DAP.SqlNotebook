namespace DAP.SqlNotebook.Contract.Entities;

public class AiSqlResponseInfo
{
    public string Sql { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}
