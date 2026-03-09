using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers;

[ApiController]
[Route("api/v1/workspaces")]
[Authorize]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IMapper _mapper;

    public WorkspacesController(
        IWorkspaceRepository workspaceRepository,
        IMapper mapper)
    {
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>List workspaces/folders for the current user (owner). Used by "My workspaces" page.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkspaceInfo>>> GetList(CancellationToken ct)
    {
        var login = User.Identity?.Name;
        var entities = await _workspaceRepository.GetByOwnerAsync(login, ct).ConfigureAwait(false);
        var list = entities.Select(e => _mapper.Map<WorkspaceInfo>(e)).ToList();
        return Ok(list);
    }

    /// <summary>All workspace/folder nodes for the common tree (hierarchy with folders).</summary>
    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceInfo>>> GetTree(CancellationToken ct)
    {
        var entities = await _workspaceRepository.GetTreeAsync(ct).ConfigureAwait(false);
        var list = entities.Select(e => _mapper.Map<WorkspaceInfo>(e)).ToList();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceInfo>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await _workspaceRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (entity == null)
            return NotFound();
        var login = User.Identity?.Name;
        if (!string.IsNullOrEmpty(login) && entity.OwnerLogin != null && entity.OwnerLogin != login)
            return Forbid();
        return Ok(_mapper.Map<WorkspaceInfo>(entity));
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceInfo>> Create([FromBody] WorkspaceInfo model, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        var entity = new WorkspaceEntity
        {
            Name = (model?.Name ?? string.Empty).Trim(),
            Description = string.IsNullOrWhiteSpace(model?.Description) ? null : model.Description.Trim(),
            OwnerLogin = login,
            ParentId = model?.ParentId,
            IsFolder = model?.IsFolder ?? false,
            Icon = model?.Icon,
            Visibility = model?.Visibility != null ? (int)model.Visibility : 0,
        };
        if (string.IsNullOrWhiteSpace(entity.Name))
            return BadRequest("Name is required.");
        var created = await _workspaceRepository.CreateAsync(entity, ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<WorkspaceInfo>(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkspaceInfo>> Update(Guid id, [FromBody] WorkspaceInfo model, CancellationToken ct)
    {
        if (model == null || id != model.Id)
            return BadRequest();
        var existing = await _workspaceRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
            return NotFound();
        var login = User.Identity?.Name;
        if (!string.IsNullOrEmpty(login) && existing.OwnerLogin != null && existing.OwnerLogin != login)
            return Forbid();
        existing.Name = (model.Name ?? string.Empty).Trim();
        existing.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        existing.ParentId = model.ParentId;
        existing.IsFolder = model.IsFolder;
        existing.Icon = model.Icon;
        existing.Visibility = (int)model.Visibility;
        await _workspaceRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
        return Ok(_mapper.Map<WorkspaceInfo>(existing));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _workspaceRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
            return NotFound();
        var login = User.Identity?.Name;
        if (!string.IsNullOrEmpty(login) && existing.OwnerLogin != null && existing.OwnerLogin != login)
            return Forbid();
        await _workspaceRepository.DeleteAsync(id, ct).ConfigureAwait(false);
        return NoContent();
    }
}
