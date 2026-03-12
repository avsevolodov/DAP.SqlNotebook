using System.Net.Http.Json;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class SchemaClient : ISchemaClient
{
    private readonly HttpClient _httpClient;

    public SchemaClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<SchemaDto> GetSchemaAsync(Guid? catalogNodeId, CancellationToken ct)
    {
        var route = EndpointsHelper.Schema(catalogNodeId);
        using var response = await _httpClient.GetAsync(route, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        var dto = await response.CdpReadContentAsAsync<SchemaDto>(ct);
        return dto ?? new SchemaDto { Entities = new List<SchemaEntityDto>(), Relations = new List<SchemaRelationDto>() };
    }

    public async Task<IReadOnlyList<SchemaAutocompleteItem>> AutocompleteAsync(SchemaAutocompleteRequest request, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.SchemaAutocomplete(), request, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        var list = await response.CdpReadContentAsAsync<List<SchemaAutocompleteItem>>(ct);
        return list ?? new List<SchemaAutocompleteItem>();
    }
}


