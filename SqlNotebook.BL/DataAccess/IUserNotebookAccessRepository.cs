using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess;

public interface IUserNotebookAccessRepository
{
    Task<IReadOnlyList<UserNotebookAccessEntity>> GetByNotebookIdAsync(Guid notebookId, CancellationToken ct = default);
    Task<UserNotebookAccessEntity?> GetAsync(Guid notebookId, string userLogin, CancellationToken ct = default);
    Task UpsertAsync(UserNotebookAccessEntity entry, CancellationToken ct = default);
    Task DeleteAsync(Guid notebookId, string userLogin, CancellationToken ct = default);
    Task ReplaceAllAsync(Guid notebookId, IReadOnlyList<UserNotebookAccessEntity> entries, CancellationToken ct = default);
}

