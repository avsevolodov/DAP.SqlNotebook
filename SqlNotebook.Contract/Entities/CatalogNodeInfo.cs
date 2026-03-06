namespace DAP.SqlNotebook.Contract.Entities;

/// <summary>Catalog tree node (source / database / table) for API and UI.</summary>
public class CatalogNodeInfo
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
    /// <summary>Connection string or server (for source/database nodes).</summary>
    public string? ConnectionInfo { get; set; }
    /// <summary>Database name / initial catalog (for source nodes).</summary>
    public string? DatabaseName { get; set; }
    /// <summary>Auth: "Basic" or "Kerberos".</summary>
    public string? AuthType { get; set; }
    public string? Login { get; set; }

    /// <summary>Kafka: consumer group prefix.</summary>
    public string? ConsumerGroupPrefix { get; set; }

    /// <summary>Kafka: when true, GUID is appended each time.</summary>
    public bool ConsumerGroupAutoGenerate { get; set; }
}
