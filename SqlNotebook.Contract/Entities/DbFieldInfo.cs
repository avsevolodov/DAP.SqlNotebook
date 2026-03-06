using System;

namespace DAP.SqlNotebook.Contract.Entities;

public class DbFieldInfo
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? Description { get; set; }
}
