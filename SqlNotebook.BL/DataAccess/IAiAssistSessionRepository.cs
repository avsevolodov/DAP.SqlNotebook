using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IAiAssistSessionRepository
{
    Task<IReadOnlyList<AiAssistSessionEntity>> GetByUserLoginAsync(string? userLogin, CancellationToken ct = default);
    Task<IReadOnlyList<AiAssistSessionEntity>> GetByUserAndNotebookAsync(string? userLogin, Guid? notebookId, CancellationToken ct = default);
    Task<AiAssistSessionEntity> CreateAsync(AiAssistSessionEntity entity, CancellationToken ct = default);
}
