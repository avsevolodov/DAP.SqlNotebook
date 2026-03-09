using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IAiAssistMessageRepository
{
    Task<AiAssistMessageEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AiAssistMessageEntity>> GetByNotebookIdAsync(Guid notebookId, CancellationToken ct = default);
    /// <summary>Global AI Assist: messages by user login (not tied to notebook).</summary>
    Task<IReadOnlyList<AiAssistMessageEntity>> GetByUserLoginAsync(string? userLogin, CancellationToken ct = default);
    Task<IReadOnlyList<AiAssistMessageEntity>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<AiAssistMessageEntity> CreateAsync(AiAssistMessageEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
