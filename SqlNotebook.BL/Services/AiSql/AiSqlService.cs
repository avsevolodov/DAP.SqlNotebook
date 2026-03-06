using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.AiSql;

namespace DAP.SqlNotebook.BL.Services.AiSql;

public sealed class AiSqlService : IAiSqlService
{
    private readonly IAiSqlBackend _backend;

    public AiSqlService(IAiSqlBackend backend)
    {
        _backend = backend ?? throw new System.ArgumentNullException(nameof(backend));
    }

    public Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct)
        => _backend.GenerateAsync(request, ct);

    public Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct)
        => _backend.FindTablesAsync(request, ct);

    public Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct)
        => _backend.AutocompleteAsync(request, ct);

    public Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct)
        => _backend.SuggestChartAsync(request, ct);
}
