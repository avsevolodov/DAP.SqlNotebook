using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Models;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers;

[ApiController]
[Route("api/v1/schema")]
public class SchemaController : ControllerBase
{
    private readonly ICatalogRepository _catalog;

    public SchemaController(ICatalogRepository catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>
    /// Returns schema-based autocomplete suggestions (tables and columns from catalog) for the SQL editor.
    /// Request: { "sql": "...", "line": 1, "column": 5 }. Response: array of { "label", "kind": "Table"|"Column", "detail", "insertText" }.
    /// </summary>
    [HttpPost("autocomplete")]
    public async Task<ActionResult<IReadOnlyList<SchemaAutocompleteItem>>> Autocomplete(
        [FromBody] SchemaAutocompleteRequest? request,
        CancellationToken ct)
    {
        if (request?.Sql == null)
            return Ok(Array.Empty<SchemaAutocompleteItem>());

        var prefix = GetPrefixAt(request.Sql, request.Line, request.Column);
        if (string.IsNullOrEmpty(prefix))
            return Ok(Array.Empty<SchemaAutocompleteItem>());

        var entities = await _catalog.GetEntitiesAsync(ct).ConfigureAwait(false);
        var result = new List<SchemaAutocompleteItem>();
        var prefixLower = prefix.Trim().ToLowerInvariant();
        var hasDot = prefixLower.Contains('.');

        if (!hasDot)
        {
            foreach (var e in entities)
            {
                var name = e.DisplayName ?? e.Name ?? "";
                var insert = e.Name ?? name;
                if (name.Length == 0) continue;
                if (prefixLower.Length > 0 && !name.ToLowerInvariant().StartsWith(prefixLower, StringComparison.Ordinal))
                    continue;
                result.Add(new SchemaAutocompleteItem
                {
                    Label = name,
                    Kind = "Table",
                    Detail = insert,
                    InsertText = insert
                });
            }
        }
        else
        {
            var parts = prefixLower.Split('.');
            var tablePart = parts[0].Trim();
            var columnPart = parts.Length > 1 ? parts[parts.Length - 1].Trim() : "";
            foreach (var e in entities)
            {
                var name = e.Name ?? "";
                var display = e.DisplayName ?? name;
                var tableName = display.ToLowerInvariant();
                var fullName = name.ToLowerInvariant();
                var tableMatches = tablePart.Length == 0
                    || tableName.StartsWith(tablePart, StringComparison.Ordinal)
                    || fullName.StartsWith(tablePart, StringComparison.Ordinal)
                    || fullName.Contains("[" + tablePart + "]", StringComparison.Ordinal);
                if (!tableMatches) continue;
                var fields = await _catalog.GetFieldsAsync(e.Id, ct).ConfigureAwait(false);
                foreach (var f in fields)
                {
                    var col = (f.Name ?? "").ToLowerInvariant();
                    if (col.Length == 0) continue;
                    if (columnPart.Length > 0 && !col.StartsWith(columnPart, StringComparison.Ordinal))
                        continue;
                    result.Add(new SchemaAutocompleteItem
                    {
                        Label = f.Name ?? "",
                        Kind = "Column",
                        Detail = f.DataType ?? "",
                        InsertText = f.Name ?? ""
                    });
                }
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns full schema for RAG/LLM: entities with fields, and relations (empty for now).
    /// Used by AI SQL service (Python) for schema cache and RAG index.
    /// When catalogNodeId is provided, returns only entities belonging to that source (database node).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SchemaDto>> GetSchema([FromQuery] Guid? catalogNodeId, CancellationToken ct)
    {
        IReadOnlyList<DbEntityInfo> entities;
        if (catalogNodeId.HasValue && catalogNodeId.Value != default)
            entities = await _catalog.GetEntitiesBySourceNodeAsync(catalogNodeId.Value, ct).ConfigureAwait(false);
        else
            entities = await _catalog.GetEntitiesAsync(ct).ConfigureAwait(false);
        var result = new List<SchemaEntityDto>(entities.Count);
        foreach (var e in entities)
        {
            var fields = await _catalog.GetFieldsAsync(e.Id, ct).ConfigureAwait(false);
            result.Add(new SchemaEntityDto
            {
                Id = e.Id,
                Name = e.Name ?? "",
                DisplayName = e.DisplayName,
                Description = e.Description,
                Fields = fields.Select(f => new SchemaFieldDto
                {
                    Name = f.Name ?? "",
                    DataType = f.DataType,
                    IsPrimaryKey = f.IsPrimaryKey,
                    IsNullable = f.IsNullable,
                }).ToList(),
            });
        }
        return Ok(new SchemaDto { Entities = result, Relations = new List<SchemaRelationDto>() });
    }

    private static string GetPrefixAt(string sql, int line, int column)
    {
        if (string.IsNullOrEmpty(sql) || line < 1 || column < 1)
            return "";
        var lines = sql.Split('\n');
        var lineIndex = Math.Min(line - 1, lines.Length - 1);
        var lineContent = lines[lineIndex] ?? "";
        var colIndex = Math.Min(column - 1, lineContent.Length);
        if (colIndex <= 0) return "";
        var beforeCursor = lineContent.Substring(0, colIndex);
        var match = System.Text.RegularExpressions.Regex.Match(beforeCursor, @"[\w.]+$");
        return match.Success ? match.Value : "";
    }
}

public class SchemaAutocompleteRequest
{
    public string? Sql { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}

public class SchemaAutocompleteItem
{
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "";
    public string? Detail { get; set; }
    public string InsertText { get; set; } = "";
}

public class SchemaDto
{
    public List<SchemaEntityDto> Entities { get; set; } = new();
    public List<SchemaRelationDto> Relations { get; set; } = new();
}

public class SchemaEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<SchemaFieldDto> Fields { get; set; } = new();
}

public class SchemaFieldDto
{
    public string Name { get; set; } = "";
    public string? DataType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
}

public class SchemaRelationDto
{
    public Guid? FromEntityId { get; set; }
    public Guid? ToEntityId { get; set; }
    public string? FromFieldName { get; set; }
    public string? ToFieldName { get; set; }
    public string? Name { get; set; }
}
