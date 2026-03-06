namespace DAP.SqlNotebook.Contract.Entities;

public class CreateDbEntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}
