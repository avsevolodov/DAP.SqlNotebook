using System;

namespace DAP.SqlNotebook.Contract.Entities;

public class DbEntityInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}
