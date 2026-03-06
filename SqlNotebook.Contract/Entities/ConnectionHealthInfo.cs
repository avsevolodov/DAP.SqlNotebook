namespace DAP.SqlNotebook.Contract.Entities;

public class ConnectionHealthInfo
{
    public ConnectionStatusInfo Status { get; set; }
    public string? Message { get; set; }
}
