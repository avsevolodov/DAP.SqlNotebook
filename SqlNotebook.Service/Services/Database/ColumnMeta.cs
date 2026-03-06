namespace DAP.SqlNotebook.Service.Services.Database;

/// <summary>Metadata for a column read from a database.</summary>
public sealed class ColumnMeta
{
    public string TableSchema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int OrdinalPosition { get; set; }
}
