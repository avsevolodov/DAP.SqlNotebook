namespace DAP.SqlNotebook.BL.Models;

public sealed class UpdateDbFieldParams
{
    public string? Name { get; set; }
    public string? DataType { get; set; }
    public bool? IsNullable { get; set; }
    public bool? IsPrimaryKey { get; set; }
    public string? Description { get; set; }
}
