using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.Contract;
using DAP.SqlNotebook.Contract.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers;

[ApiController]
[Route(ApiRoutes.Favorites)]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly INotebookFavoritesRepository _favoritesRepository;

    public FavoritesController(INotebookFavoritesRepository favoritesRepository)
    {
        _favoritesRepository = favoritesRepository ?? throw new ArgumentNullException(nameof(favoritesRepository));
    }

    [HttpGet("folders")]
    public async Task<ActionResult<IReadOnlyList<FavoriteFolderInfo>>> GetFolders(CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Ok(Array.Empty<FavoriteFolderInfo>());
        var list = await _favoritesRepository.GetFoldersAsync(login, ct).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpPost("folders")]
    public async Task<ActionResult<FavoriteFolderInfo>> CreateFolder([FromBody] CreateFavoriteFolderRequest request, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest("Name is required.");
        var id = await _favoritesRepository.CreateFolderAsync(login, request.Name.Trim(), request.ParentId, ct).ConfigureAwait(false);
        return Ok(new FavoriteFolderInfo { Id = id, Name = request.Name.Trim(), ParentId = request.ParentId });
    }

    [HttpDelete("folders/{folderId:guid}")]
    public async Task<ActionResult> DeleteFolder(Guid folderId, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized();
        await _favoritesRepository.DeleteFolderAsync(login, folderId, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("notebooks")]
    public async Task<ActionResult<IReadOnlyList<FavoriteNotebookItemInfo>>> GetFavoriteNotebooks(CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Ok(Array.Empty<FavoriteNotebookItemInfo>());
        var list = await _favoritesRepository.GetFavoriteNotebooksAsync(login, ct).ConfigureAwait(false);
        return Ok(list);
    }

    [HttpPost("notebooks/{notebookId:guid}")]
    public async Task<ActionResult> AddNotebookToFavorites(Guid notebookId, [FromBody] AddNotebookToFavoritesRequest? request, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized();
        await _favoritesRepository.AddNotebookToFavoritesAsync(login, notebookId, request?.FolderId, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpDelete("notebooks/{notebookId:guid}")]
    public async Task<ActionResult> RemoveNotebookFromFavorites(Guid notebookId, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized();
        await _favoritesRepository.RemoveNotebookFromFavoritesAsync(login, notebookId, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPut("notebooks/{notebookId:guid}/folder")]
    public async Task<ActionResult> SetNotebookFolder(Guid notebookId, [FromBody] SetNotebookFolderRequest? request, CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized();
        await _favoritesRepository.SetNotebookFolderAsync(login, notebookId, request?.FolderId, ct).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("notebooks/ids")]
    public async Task<ActionResult<object>> GetFavoriteNotebookIds(CancellationToken ct)
    {
        var login = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(login))
            return Ok(Array.Empty<Guid>());
        var set = await _favoritesRepository.GetFavoriteNotebookIdsAsync(login, ct).ConfigureAwait(false);
        return Ok(set);
    }
}

public class CreateFavoriteFolderRequest
{
    public string? Name { get; set; }
    public Guid? ParentId { get; set; }
}

public class AddNotebookToFavoritesRequest
{
    public Guid? FolderId { get; set; }
}

public class SetNotebookFolderRequest
{
    public Guid? FolderId { get; set; }
}
