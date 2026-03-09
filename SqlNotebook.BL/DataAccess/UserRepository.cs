using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess;

public sealed class UserRepository : IUserRepository
{
    private readonly SqlNotebookDbContext _db;

    public UserRepository(SqlNotebookDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<UserEntity?> GetByLoginAsync(string login, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login))
            return null;
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Login == login, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking().OrderBy(x => x.Login).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<UserEntity> CreateAsync(UserEntity entity, CancellationToken ct = default)
    {
        if (entity.Id == default)
            entity.Id = Guid.NewGuid();
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAsync(UserEntity entity, CancellationToken ct = default)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(x => x.Id == entity.Id, ct).ConfigureAwait(false);
        if (existing == null)
            throw new InvalidOperationException($"User {entity.Id} not found.");
        existing.Login = entity.Login;
        existing.Role = entity.Role;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (entity != null)
        {
            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
