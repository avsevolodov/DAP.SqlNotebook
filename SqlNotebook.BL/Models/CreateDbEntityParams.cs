namespace DAP.SqlNotebook.BL.Models;

public sealed class CreateDbEntityParams
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}
