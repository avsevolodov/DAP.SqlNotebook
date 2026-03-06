using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAP.SqlNotebook.BL.DataAccess
{
    public class NotebookRepository : INotebookRepository
    {
        private readonly SqlNotebookDbContext _db;

        public NotebookRepository(SqlNotebookDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<NotebookEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Notebooks
                .AsNoTracking()
                .Include(x => x.Cells.OrderBy(c => c.OrderIndex))
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<NotebookEntity>> GetListAsync(int offset, int batchSize, Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            IQueryable<NotebookEntity> query = _db.Notebooks.AsNoTracking().OrderByDescending(x => x.UpdatedAt);
            if (workspaceId.HasValue)
                query = query.Where(x => x.WorkspaceId == workspaceId.Value);
            return await query
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> GetTotalCountAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            if (workspaceId.HasValue)
                return await _db.Notebooks.CountAsync(x => x.WorkspaceId == workspaceId.Value, cancellationToken).ConfigureAwait(false);
            return await _db.Notebooks.CountAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<NotebookEntity> CreateAsync(NotebookEntity notebook, CancellationToken cancellationToken = default)
        {
            if (notebook.Id == default)
                notebook.Id = Guid.NewGuid();

            var now = DateTime.UtcNow;
            notebook.CreatedAt = now;
            notebook.UpdatedAt = now;

            for (var i = 0; i < notebook.Cells.Count; i++)
            {
                var cell = notebook.Cells.ElementAt(i);
                cell.NotebookId = notebook.Id;
                cell.OrderIndex = i;
                cell.Id = 0; // let DB generate
            }

            _db.Notebooks.Add(notebook);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return notebook;
        }

        public async Task UpdateAsync(NotebookEntity notebook, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Notebooks
                .Include(x => x.Cells)
                .FirstOrDefaultAsync(x => x.Id == notebook.Id, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
                throw new InvalidOperationException($"Notebook with id {notebook.Id} not found.");

            existing.Name = notebook.Name;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.WorkspaceId = notebook.WorkspaceId;
            existing.CatalogNodeId = notebook.CatalogNodeId;
            existing.CatalogNodeDisplayName = notebook.CatalogNodeDisplayName;
            existing.UpdatedBy = notebook.UpdatedBy;

            // Replace cells: remove existing, add from notebook
            _db.NotebookCells.RemoveRange(existing.Cells);

            var cells = notebook.Cells.OrderBy(c => c.OrderIndex).ToList();
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var newCell = new NotebookCellEntity
                {
                    NotebookId = notebook.Id,
                    OrderIndex = i,
                    CellType = cell.CellType,
                    Content = cell.Content,
                    ExecutionResultJson = cell.ExecutionResultJson,
                    CreatedBy = cell.CreatedBy,
                    CatalogNodeId = cell.CatalogNodeId,
                    DatabaseDisplayName = cell.DatabaseDisplayName,
                };
                existing.Cells.Add(newCell);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var notebook = await _db.Notebooks.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            if (notebook == null)
                return;

            _db.Notebooks.Remove(notebook);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
