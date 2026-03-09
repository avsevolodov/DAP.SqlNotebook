namespace DAP.SqlNotebook.Service.Services.Database;

/// <summary>Metadata for a table read from a database.</summary>
public sealed class TableMeta
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QualifiedName { get; set; } = string.Empty;
}
