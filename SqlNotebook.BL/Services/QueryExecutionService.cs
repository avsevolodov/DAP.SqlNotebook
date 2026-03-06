using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.Services;

public interface IQueryExecutionService
{
    Task<NotebookCellExecutionResultInfo> ExecuteAsync(string query, int timeoutSeconds, CancellationToken ct);
}

public sealed class QueryExecutionService : IQueryExecutionService
{
    private readonly IQueryExecutor _executor;

    public QueryExecutionService(IQueryExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public async Task<NotebookCellExecutionResultInfo> ExecuteAsync(string query, int timeoutSeconds, CancellationToken ct)
    {
        try
        {
            var result = await _executor.ExecuteAsync(query, timeoutSeconds, ct);
            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Success,
                Columns = result.ColumnNames.Select(n => new ExecutionResultColumnInfo { Name = n }).ToArray(),
                Rows = result.Rows.Select(values => new ExecutionResultRowInfo { Values = values.ToList() }).ToArray(),
            };
        }
        catch (OperationCanceledException)
        {
            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Failed,
                Error = $"Query timed out after {timeoutSeconds} seconds.",
            };
        }
        catch (Exception ex)
        {
            return new NotebookCellExecutionResultInfo
            {
                Status = NotebookCellExecutionStatusInfo.Failed,
                Error = ex.Message,
            };
        }
    }
}
