using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services;
using DAP.SqlNotebook.BL.Services;

namespace DAP.SqlNotebook.Service.Services.Database;

/// <summary>
/// Strategy for a specific database provider: connection string building, health check, and schema metadata reading.
/// </summary>
public interface IDbProviderStrategy
{
    /// <summary>Provider key (e.g. "MSSQL", "CLICKHOUSE"). Case-insensitive match.</summary>
    string ProviderKey { get; }

    /// <summary>Build a full connection string. If value contains "=", treat as existing connection string and optionally append Database=.
    /// For Basic auth pass authType="Basic", login and password; for Kerberos use authType="Kerberos" or null (Integrated Security).</summary>
    string BuildConnectionString(string value, string? databaseName, string? authType = null, string? login = null, string? password = null);

    /// <summary>Check connectivity; returns status 1 = OK, 2 = error, 0 = not supported / no config.</summary>
    Task<ConnectionHealthResult> CheckAsync(string connectionString, CancellationToken ct = default);

    /// <summary>Read tables and columns from the database.</summary>
    Task<(List<TableMeta> Tables, List<ColumnMeta> Columns)> ReadMetadataAsync(string connectionString, CancellationToken ct = default);

    /// <summary>Execute a query and return result set (column names + rows). For SELECT only; throws on error. Stops after maxRows when specified.</summary>
    Task<QueryResult> ExecuteQueryAsync(string connectionString, string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default);
}
