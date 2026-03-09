using System;

namespace DAP.SqlNotebook.BL.Models;

public sealed class CatalogNode
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Owner { get; set; }
    public string? Provider { get; set; }
    public bool HasChildren { get; set; }
    public Guid? EntityId { get; set; }
    public string? QualifiedName { get; set; }
    public string? ConnectionInfo { get; set; }
    public string? DatabaseName { get; set; }
    public string? AuthType { get; set; }
    public string? Login { get; set; }
    public string? PasswordEncrypted { get; set; }

    public string? ConsumerGroupPrefix { get; set; }
    public bool ConsumerGroupAutoGenerate { get; set; }
}
