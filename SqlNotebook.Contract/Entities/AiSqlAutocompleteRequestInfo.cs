namespace DAP.SqlNotebook.Contract.Entities;

public class AiSqlAutocompleteRequestInfo
{
    public string Sql { get; set; } = string.Empty;
    public List<string>? Entities { get; set; }
}
