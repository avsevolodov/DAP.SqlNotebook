using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class AiAssistMessageRepository : IAiAssistMessageRepository
{
    private readonly SqlNotebookDbContext _db;

    public AiAssistMessageRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<AiAssistMessageEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.AiAssistMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiAssistMessageEntity>> GetByNotebookIdAsync(Guid notebookId, CancellationToken ct = default)
    {
        return await _db.AiAssistMessages.AsNoTracking()
            .Where(x => x.NotebookId == notebookId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiAssistMessageEntity>> GetByUserLoginAsync(string? userLogin, CancellationToken ct = default)
    {
        return await _db.AiAssistMessages.AsNoTracking()
            .Where(x => x.UserLogin == (userLogin ?? ""))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiAssistMessageEntity>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.AiAssistMessages.AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<AiAssistMessageEntity> CreateAsync(AiAssistMessageEntity entity, CancellationToken ct = default)
    {
        if (entity.Id == default)
            entity.Id = Guid.NewGuid();
        _db.AiAssistMessages.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.AiAssistMessages.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (entity != null)
        {
            _db.AiAssistMessages.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
