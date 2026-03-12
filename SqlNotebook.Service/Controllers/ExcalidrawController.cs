using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.Notebook;
using DAP.SqlNotebook.Contract;
using DAP.SqlNotebook.Contract.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers
{
    /// <summary>
    /// API for Excalidraw diagram storage. Diagrams are stored in notebook cells of type Excalidraw.
    /// </summary>
    [ApiController]
    [Route(ApiRoutes.Excalidraw)]
    [Authorize]
    public class ExcalidrawController : ControllerBase
    {
        private readonly INotebookManager _notebookManager;

        public ExcalidrawController(INotebookManager notebookManager)
        {
            _notebookManager = notebookManager ?? throw new ArgumentNullException(nameof(notebookManager));
        }

        /// <summary>
        /// Get Excalidraw content for a cell.
        /// </summary>
        [HttpGet("{cellId:int}")]
        public async Task<ActionResult<string>> Get(Guid notebookId, int cellId, CancellationToken ct)
        {
            var notebook = await _notebookManager.GetByIdAsync(notebookId, ct).ConfigureAwait(false);
            if (notebook?.Cells == null)
                return NotFound();

            var cell = notebook.Cells.FirstOrDefault(c => c.Id == cellId && c.Type == NotebookCellTypeInfo.Excalidraw);
            if (cell == null)
                return NotFound();

            return Ok(cell.Content ?? "{}");
        }

        /// <summary>
        /// Save Excalidraw content for a cell.
        /// </summary>
        [HttpPut("{cellId:int}")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult> Put(Guid notebookId, int cellId, [FromBody] ExcalidrawContentRequest request, CancellationToken ct)
        {
            var content = request?.Content;
            if (content == null)
                return BadRequest();

            var notebook = await _notebookManager.GetByIdAsync(notebookId, ct).ConfigureAwait(false);
            if (notebook?.Cells == null)
                return NotFound();

            var cell = notebook.Cells.FirstOrDefault(c => c.Id == cellId && c.Type == NotebookCellTypeInfo.Excalidraw);
            if (cell == null)
                return NotFound();

            cell.Content = content;
            var login = User.Identity?.Name;
            await _notebookManager.UpdateAsync(notebookId, notebook, login, ct).ConfigureAwait(false);
            return NoContent();
        }
    }

   
}
