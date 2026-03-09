namespace DAP.SqlNotebook.Contract.Entities;

public class SuggestChartResponseInfo
{
    public string ChartType { get; set; } = "table";
    public string? Reason { get; set; }
}
