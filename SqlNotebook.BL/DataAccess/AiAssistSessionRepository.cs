using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class AiAssistSessionRepository : IAiAssistSessionRepository
{
    private readonly SqlNotebookDbContext _db;

    public AiAssistSessionRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<AiAssistSessionEntity>> GetByUserLoginAsync(string? userLogin, CancellationToken ct = default)
    {
        return await _db.AiAssistSessions.AsNoTracking()
            .Where(x => x.UserLogin == (userLogin ?? "") && x.NotebookId == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiAssistSessionEntity>> GetByUserAndNotebookAsync(string? userLogin, Guid? notebookId, CancellationToken ct = default)
    {
        var query = _db.AiAssistSessions.AsNoTracking().Where(x => x.UserLogin == (userLogin ?? ""));
        if (notebookId.HasValue && notebookId.Value != default)
            query = query.Where(x => x.NotebookId == notebookId.Value);
        else
            query = query.Where(x => x.NotebookId == null);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<AiAssistSessionEntity> CreateAsync(AiAssistSessionEntity entity, CancellationToken ct = default)
    {
        if (entity.Id == default)
            entity.Id = Guid.NewGuid();
        _db.AiAssistSessions.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }
}
