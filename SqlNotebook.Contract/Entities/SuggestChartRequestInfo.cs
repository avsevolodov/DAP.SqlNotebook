namespace DAP.SqlNotebook.Contract.Entities;

public class SuggestChartRequestInfo
{
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}
