using System;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.Services;

public interface ISchemaImportService
{
    Task<SchemaImportResult> ImportStructureAsync(Guid nodeId, CancellationToken ct = default);
}

public sealed class SchemaImportResult
{
    public int TablesCount { get; set; }
    public int FieldsCount { get; set; }
    public string? Error { get; set; }
}
