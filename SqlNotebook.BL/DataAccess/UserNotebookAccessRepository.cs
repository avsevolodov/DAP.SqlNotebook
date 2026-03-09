using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class UserNotebookAccessRepository : IUserNotebookAccessRepository
{
    private readonly SqlNotebookDbContext _db;

    public UserNotebookAccessRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<UserNotebookAccessEntity>> GetByNotebookIdAsync(Guid notebookId, CancellationToken ct = default)
    {
        return await _db.UserNotebookAccess
            .AsNoTracking()
            .Where(x => x.NotebookId == notebookId)
            .OrderBy(x => x.UserLogin)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<UserNotebookAccessEntity?> GetAsync(Guid notebookId, string userLogin, CancellationToken ct = default)
    {
        userLogin ??= "";
        if (string.IsNullOrWhiteSpace(userLogin))
            return null;

        return await _db.UserNotebookAccess
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NotebookId == notebookId && x.UserLogin == userLogin, ct)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(UserNotebookAccessEntity entry, CancellationToken ct = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (entry.NotebookId == default) throw new ArgumentException("NotebookId is required.", nameof(entry));
        entry.UserLogin ??= "";
        if (string.IsNullOrWhiteSpace(entry.UserLogin)) throw new ArgumentException("UserLogin is required.", nameof(entry));

        var existing = await _db.UserNotebookAccess
            .FirstOrDefaultAsync(x => x.NotebookId == entry.NotebookId && x.UserLogin == entry.UserLogin, ct)
            .ConfigureAwait(false);

        if (existing == null)
        {
            entry.CreatedAt = DateTime.UtcNow;
            _db.UserNotebookAccess.Add(entry);
        }
        else
        {
            existing.Role = entry.Role;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid notebookId, string userLogin, CancellationToken ct = default)
    {
        userLogin ??= "";
        if (notebookId == default || string.IsNullOrWhiteSpace(userLogin))
            return;

        var existing = await _db.UserNotebookAccess
            .FirstOrDefaultAsync(x => x.NotebookId == notebookId && x.UserLogin == userLogin, ct)
            .ConfigureAwait(false);

        if (existing == null)
            return;

        _db.UserNotebookAccess.Remove(existing);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ReplaceAllAsync(Guid notebookId, IReadOnlyList<UserNotebookAccessEntity> entries, CancellationToken ct = default)
    {
        if (notebookId == default)
            throw new ArgumentException("NotebookId is required.", nameof(notebookId));

        var existing = await _db.UserNotebookAccess
            .Where(x => x.NotebookId == notebookId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (existing.Count > 0)
            _db.UserNotebookAccess.RemoveRange(existing);

        if (entries != null && entries.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.UserLogin)) continue;
                var toAdd = new UserNotebookAccessEntity
                {
                    NotebookId = notebookId,
                    UserLogin = e.UserLogin.Trim(),
                    Role = e.Role,
                    CreatedAt = now,
                };
                _db.UserNotebookAccess.Add(toAdd);
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

