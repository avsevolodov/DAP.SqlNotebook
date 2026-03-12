using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class AiSqlClient : IAiSqlClient
{
    private readonly HttpClient _httpClient;

    public AiSqlClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<SuggestChartResponseInfo?> SuggestChartAsync(SuggestChartRequestInfo request, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.AiSqlSuggestChart(), request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.CdpReadContentAsAsync<SuggestChartResponseInfo>(ct);
    }

    public async Task<AiSqlResponseInfo?> GenerateAsync(AiSqlRequestInfo request, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.AiSqlGenerate(), request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.CdpReadContentAsAsync<AiSqlResponseInfo>(ct);
    }

    public async Task<FindTablesResponseInfo?> FindTablesAsync(FindTablesRequestInfo request, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.AiSqlFindTables(), request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.CdpReadContentAsAsync<FindTablesResponseInfo>(ct);
    }
}
