namespace DAP.SqlNotebook.BL.Models;

public sealed class UpdateCatalogNodeParams
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Owner { get; set; }
    public string? Provider { get; set; }
    public string? ConnectionInfo { get; set; }
    public string? DatabaseName { get; set; }
    public string? AuthType { get; set; }
    public string? Login { get; set; }
    public string? PasswordEncrypted { get; set; }
}
