namespace DAP.SqlNotebook.BL.Models;

public sealed class UpdateDbEntityParams
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? SchemaName { get; set; }
    public string? Description { get; set; }
}
