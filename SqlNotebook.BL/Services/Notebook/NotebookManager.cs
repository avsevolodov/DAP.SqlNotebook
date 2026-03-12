using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;

namespace DAP.SqlNotebook.BL.Services.Notebook;

public sealed class NotebookManager : INotebookManager
{
    private readonly INotebookRepository _repository;
    private readonly IQueryExecutionService _queryExecutionService;
    private readonly IMapper _mapper;

    public NotebookManager(INotebookRepository repository, IQueryExecutionService queryExecutionService, IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _queryExecutionService = queryExecutionService ?? throw new ArgumentNullException(nameof(queryExecutionService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<(IReadOnlyList<NotebookMetaInfo> Notebooks, int TotalCount)> GetListAsync(
        int offset,
        int batchSize,
        Guid? workspaceId,
        string? userLogin,
        NotebookStatusInfo? status = null,
        CancellationToken ct = default)
    {
        var statusInt = status.HasValue ? (int)status.Value : (int?)null;
        var items = await _repository.GetListAsync(offset, batchSize, userLogin, workspaceId, statusInt, ct).ConfigureAwait(false);
        var total = await _repository.GetTotalCountAsync(userLogin, workspaceId, statusInt, ct).ConfigureAwait(false);
        var notebooks = items.Select(e => _mapper.Map<NotebookMetaInfo>(e)).ToList();
        return (notebooks, total);
    }

    public async Task<NotebookInfo?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        return entity == null ? null : _mapper.Map<NotebookInfo>(entity);
    }

    public async Task<NotebookInfo> CreateAsync(NotebookInfo model, string? userLogin, CancellationToken ct)
    {
        var entity = _mapper.Map<NotebookEntity>(model);
        if (entity.Id == default)
            entity.Id = Guid.NewGuid();
        entity.CreatedBy = userLogin;
        entity.UpdatedBy = userLogin;
        foreach (var c in entity.Cells)
            c.CreatedBy = userLogin;

        await _repository.CreateAsync(entity, ct).ConfigureAwait(false);
        var created = await _repository.GetByIdAsync(entity.Id, ct).ConfigureAwait(false);
        return _mapper.Map<NotebookInfo>(created!);
    }

    public async Task<NotebookInfo> UpdateAsync(Guid id, NotebookInfo model, string? userLogin, CancellationToken ct)
    {
        var entity = _mapper.Map<NotebookEntity>(model);
        entity.Id = id;
        entity.UpdatedBy = userLogin;
        foreach (var c in entity.Cells)
        {
            if (c.Id == 0)
                c.CreatedBy = userLogin;
        }

        await _repository.UpdateAsync(entity, ct).ConfigureAwait(false);
        var updated = await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        return _mapper.Map<NotebookInfo>(updated!);
    }

    public Task SetStatusAsync(Guid id, NotebookStatusInfo status, CancellationToken ct = default)
        => _repository.SetStatusAsync(id, (int)status, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct) => _repository.DeleteAsync(id, ct);

    public Task<NotebookCellExecutionResultInfo> ExecuteQueryAsync(string query, int timeoutSeconds, int? maxRows = null, CancellationToken ct = default)
        => _queryExecutionService.ExecuteAsync(query, timeoutSeconds, maxRows, ct);
}
