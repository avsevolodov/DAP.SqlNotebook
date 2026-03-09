namespace DAP.SqlNotebook.Contract.Entities;

public class UpdateDbFieldInfo
{
    public string? Name { get; set; }
    public string? DataType { get; set; }
    public bool? IsNullable { get; set; }
    public bool? IsPrimaryKey { get; set; }
    public string? Description { get; set; }
}
