using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.Notebook;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DAP.SqlNotebook.Service.Controllers
{
    [ApiController]
    [Route("api/v1/notebooks")]
    [Authorize]
    public class NotebooksController : ControllerBase
    {
        private readonly INotebookManager _notebookManager;
        private readonly INodeQueryExecutorService _nodeQueryExecutor;
        private readonly IConfiguration _configuration;

        public NotebooksController(INotebookManager notebookManager, INodeQueryExecutorService nodeQueryExecutor, IConfiguration configuration)
        {
            _notebookManager = notebookManager ?? throw new ArgumentNullException(nameof(notebookManager));
            _nodeQueryExecutor = nodeQueryExecutor ?? throw new ArgumentNullException(nameof(nodeQueryExecutor));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpGet]
        public async Task<ActionResult<NotebooksResponse>> GetList(
            CancellationToken ct,
            [FromQuery] int offset = 0,
            [FromQuery] int batchSize = 100,
            [FromQuery] Guid? workspaceId = null)
        {
            var (notebooks, total) = await _notebookManager.GetListAsync(offset, batchSize, workspaceId, ct).ConfigureAwait(false);
            return Ok(new NotebooksResponse { Notebooks = notebooks.ToList(), TotalCount = total });
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NotebookInfo>> GetById(Guid id, CancellationToken ct)
        {
            var notebook = await _notebookManager.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (notebook == null)
                return NotFound();
            return Ok(notebook);
        }

        [HttpPost]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult<NotebookInfo>> Create([FromBody] NotebookInfo model, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            var created = await _notebookManager.CreateAsync(model, login, ct).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult<NotebookInfo>> Update(Guid id, [FromBody] NotebookInfo model, CancellationToken ct)
        {
            if (id != model.Id)
                return BadRequest();
            var login = User.Identity?.Name;
            var updated = await _notebookManager.UpdateAsync(id, model, login, ct).ConfigureAwait(false);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _notebookManager.DeleteAsync(id, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpPost("{id:guid}/execute")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult<NotebookCellExecutionResultInfo>> Execute(
            Guid id,
            [FromBody] ConnectionExecutionRequest request,
            CancellationToken ct)
        {
            if (request == null)
                return BadRequest();
            // Allow empty query when running against a catalog node (e.g. Kafka topic: fetch latest messages).
            if (string.IsNullOrWhiteSpace(request.Query) && !request.CatalogNodeId.HasValue)
                return BadRequest();

            var defaultTimeoutSeconds = _configuration.GetSection("SqlNotebook").GetValue("DefaultCommandTimeoutSeconds", 30);
            var timeoutSeconds = request.CommandTimeoutSeconds ?? defaultTimeoutSeconds;
            timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 600);

            var queryToRun = request.Query?.Trim() ?? "";
            NotebookCellExecutionResultInfo result;
            if (request.CatalogNodeId.HasValue)
            {
                result = await _nodeQueryExecutor.ExecuteAsync(request.CatalogNodeId.Value, queryToRun, timeoutSeconds, ct).ConfigureAwait(false);
            }
            else
            {
                result = await _notebookManager.ExecuteQueryAsync(queryToRun, timeoutSeconds, ct).ConfigureAwait(false);
            }
            return Ok(result);
        }
    }
}
