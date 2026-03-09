namespace DAP.SqlNotebook.Contract.Entities;

public class SchemaImportResultInfo
{
    public int TablesCount { get; set; }
    public int FieldsCount { get; set; }
    public string? Error { get; set; }
}
