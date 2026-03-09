using System.Net.Http.Json;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client;
using DAP.SqlNotebook.Service.Client.Exceptions;

namespace DAP.SqlNotebook.Service.Client.V1;

public class AiAssistClient : IAiAssistClient
{
    private readonly HttpClient _httpClient;

    public AiAssistClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<AiAssistSessionInfo>> GetSessionsAsync(Guid? notebookId, CancellationToken ct)
    {
        var route = EndpointsHelper.AiAssistSessions(notebookId);
        var list = await _httpClient.GetFromJsonAsync<List<AiAssistSessionInfo>>(route, ct);
        return list ?? new List<AiAssistSessionInfo>();
    }

    public async Task<AiAssistSessionInfo> CreateSessionAsync(AiAssistSessionInfo model, CancellationToken ct)
    {
        var route = $"{EndpointsHelper.AiAssist}/sessions";
        using var response = await _httpClient.PostAsJsonAsync(route, model, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<AiAssistSessionInfo>(ct);
    }

    public async Task<IReadOnlyList<AiAssistMessageInfo>> GetMessagesAsync(Guid? sessionId, CancellationToken ct)
    {
        var route = EndpointsHelper.AiAssistMessages(sessionId);
        var list = await _httpClient.GetFromJsonAsync<List<AiAssistMessageInfo>>(route, ct);
        return list ?? new List<AiAssistMessageInfo>();
    }

    public async Task<AiAssistSendResponseInfo> SendAsync(AiAssistSendRequestInfo request, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync(EndpointsHelper.AiAssistSend(), request, ct);
        await response.ManagementServiceEnsureSuccessStatusCode();
        return await response.CdpReadContentAsAsync<AiAssistSendResponseInfo>(ct);
    }
}
