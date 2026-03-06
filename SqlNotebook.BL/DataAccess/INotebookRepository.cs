using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;

namespace DAP.SqlNotebook.BL.DataAccess
{
    public interface INotebookRepository
    {
        Task<NotebookEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NotebookEntity>> GetListAsync(int offset, int batchSize, Guid? workspaceId = null, CancellationToken cancellationToken = default);

        Task<int> GetTotalCountAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default);

        Task<NotebookEntity> CreateAsync(NotebookEntity notebook, CancellationToken cancellationToken = default);

        Task UpdateAsync(NotebookEntity notebook, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
