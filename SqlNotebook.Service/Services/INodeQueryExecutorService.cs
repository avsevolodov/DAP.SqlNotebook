using System;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Services;

/// <summary>
/// Executes a SQL query against the connection of a catalog node (database/source).
/// Used when the user selects a database from the dropdown in the editor.
/// </summary>
public interface INodeQueryExecutorService
{
    Task<NotebookCellExecutionResultInfo> ExecuteAsync(Guid catalogNodeId, string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default);
}
