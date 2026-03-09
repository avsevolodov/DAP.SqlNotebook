using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserEntity?> GetByLoginAsync(string login, CancellationToken ct = default);
    Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken ct = default);
    Task<UserEntity> CreateAsync(UserEntity entity, CancellationToken ct = default);
    Task UpdateAsync(UserEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
