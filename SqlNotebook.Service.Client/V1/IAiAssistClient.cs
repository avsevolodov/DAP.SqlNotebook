using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.Service.Client.V1;

public interface IAiAssistClient
{
    Task<IReadOnlyList<AiAssistSessionInfo>> GetSessionsAsync(Guid? notebookId, CancellationToken ct);
    Task<AiAssistSessionInfo> CreateSessionAsync(AiAssistSessionInfo model, CancellationToken ct);
    Task<IReadOnlyList<AiAssistMessageInfo>> GetMessagesAsync(Guid? sessionId, CancellationToken ct);
    Task<AiAssistSendResponseInfo> SendAsync(AiAssistSendRequestInfo request, CancellationToken ct);
}
