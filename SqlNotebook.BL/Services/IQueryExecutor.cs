using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.Services;

/// <summary>
/// Abstraction for executing ad-hoc SQL. Implemented by the host (e.g. Service) with the actual connection.
/// </summary>
public interface IQueryExecutor
{
    Task<QueryResult> ExecuteAsync(string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default);
}

public sealed class QueryResult
{
    public IReadOnlyList<string> ColumnNames { get; init; } = null!;
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = null!;
}
