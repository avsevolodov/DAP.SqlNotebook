namespace DAP.SqlNotebook.Contract.Entities;

public class CatalogNodeUpdateInfo
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Owner { get; set; }
    public string? Provider { get; set; }
    public string? ConnectionInfo { get; set; }
    public string? DatabaseName { get; set; }
    public string? AuthType { get; set; }
    public string? Login { get; set; }
    /// <summary>If set, update stored password (Basic auth).</summary>
    public string? Password { get; set; }
    public string? ConsumerGroupPrefix { get; set; }
    public bool? ConsumerGroupAutoGenerate { get; set; }
}
