using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface IAiSqlClient
{
    Task<SuggestChartResponseInfo?> SuggestChartAsync(SuggestChartRequestInfo request, CancellationToken ct);
    Task<AiSqlResponseInfo?> GenerateAsync(AiSqlRequestInfo request, CancellationToken ct);
    Task<FindTablesResponseInfo?> FindTablesAsync(FindTablesRequestInfo request, CancellationToken ct);
}
