using System;
using System.Collections.Generic;
using System.Linq;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

/// <summary>
/// In-memory representation of tables, columns and relations for autocomplete.
/// Expected to be built once from external schema API and reused.
/// </summary>
public sealed class SchemaGraph
{
    private readonly Dictionary<string, SchemaTable> _tables;
    private readonly List<SchemaRelation> _relations;

    public SchemaGraph(IEnumerable<SchemaTable> tables, IEnumerable<SchemaRelation> relations)
    {
        _tables = tables?.ToDictionary(
            t => t.Name,
            t => t,
            StringComparer.OrdinalIgnoreCase
        ) ?? new Dictionary<string, SchemaTable>(StringComparer.OrdinalIgnoreCase);

        _relations = relations?.ToList() ?? new List<SchemaRelation>();
    }

    public IReadOnlyCollection<SchemaTable> Tables => _tables.Values;
    public IReadOnlyCollection<SchemaRelation> Relations => _relations;

    public SchemaTable? TryGetTable(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return _tables.TryGetValue(name, out var t) ? t : null;
    }

    public IReadOnlyList<SchemaRelation> GetOutgoingRelations(string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return Array.Empty<SchemaRelation>();

        return _relations
            .Where(r => string.Equals(r.FromTable, tableName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static string SuggestAlias(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return "t";
        var parts = tableName.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var s = parts[0];
            if (s.Length <= 2)
                return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s[^1..].ToLowerInvariant();
        }

        return string.Concat(parts.Select(p => char.ToLowerInvariant(p[0])));
    }
}

public sealed class SchemaTable
{
    public string Name { get; }
    public string? Description { get; }
    public IReadOnlyList<SchemaColumn> Columns { get; }

    public SchemaTable(string name, string? description, IEnumerable<SchemaColumn> columns)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Columns = (columns ?? Array.Empty<SchemaColumn>()).ToList();
    }
}

public sealed class SchemaColumn
{
    public string Name { get; }
    public string? DataType { get; }
    public string? Description { get; }

    public SchemaColumn(string name, string? dataType, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType;
        Description = description;
    }
}

public sealed class SchemaRelation
{
    public string FromTable { get; }
    public string FromColumn { get; }
    public string ToTable { get; }
    public string ToColumn { get; }

    public SchemaRelation(string fromTable, string fromColumn, string toTable, string toColumn)
    {
        FromTable = fromTable ?? throw new ArgumentNullException(nameof(fromTable));
        FromColumn = fromColumn ?? throw new ArgumentNullException(nameof(fromColumn));
        ToTable = toTable ?? throw new ArgumentNullException(nameof(toTable));
        ToColumn = toColumn ?? throw new ArgumentNullException(nameof(toColumn));
    }
}

