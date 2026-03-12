using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.Notebook;
using DAP.SqlNotebook.BL.Services.NotebookAccess;
using DAP.SqlNotebook.Contract;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DAP.SqlNotebook.Service.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Notebooks)]
    [Authorize]
    public class NotebooksController : ControllerBase
    {
        private readonly INotebookManager _notebookManager;
        private readonly INotebookAccessManager _accessManager;
        private readonly INodeQueryExecutorService _nodeQueryExecutor;
        private readonly IConfiguration _configuration;

        public NotebooksController(INotebookManager notebookManager, INotebookAccessManager accessManager, INodeQueryExecutorService nodeQueryExecutor, IConfiguration configuration)
        {
            _notebookManager = notebookManager ?? throw new ArgumentNullException(nameof(notebookManager));
            _accessManager = accessManager ?? throw new ArgumentNullException(nameof(accessManager));
            _nodeQueryExecutor = nodeQueryExecutor ?? throw new ArgumentNullException(nameof(nodeQueryExecutor));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpGet]
        public async Task<ActionResult<NotebooksResponse>> GetList(
            CancellationToken ct,
            [FromQuery] int offset = 0,
            [FromQuery] int batchSize = 100,
            [FromQuery] Guid? workspaceId = null,
            [FromQuery] NotebookStatusInfo? status = null)
        {
            var login = User.Identity?.Name;
            var (notebooks, total) = await _notebookManager.GetListAsync(offset, batchSize, workspaceId, login, status, ct).ConfigureAwait(false);
            return Ok(new NotebooksResponse { Notebooks = notebooks.ToList(), TotalCount = total });
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NotebookInfo>> GetById(Guid id, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            if (!await _accessManager.CanViewAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();
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
            if (!await _accessManager.CanEditAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();
            var updated = await _notebookManager.UpdateAsync(id, model, login, ct).ConfigureAwait(false);
            return Ok(updated);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult> SetStatus(Guid id, [FromBody] SetNotebookStatusRequest request, CancellationToken ct)
        {
            if (request?.Status == null)
                return BadRequest("Status is required.");
            var login = User.Identity?.Name;
            if (!await _accessManager.CanEditAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();
            await _notebookManager.SetStatusAsync(id, request.Status.Value, ct).ConfigureAwait(false);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            if (!await _accessManager.IsOwnerAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();
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

            var login = User.Identity?.Name;
            if (!await _accessManager.CanEditAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();

            var defaultTimeoutSeconds = _configuration.GetSection("SqlNotebook").GetValue("DefaultCommandTimeoutSeconds", 30);
            var timeoutSeconds = request.CommandTimeoutSeconds ?? defaultTimeoutSeconds;
            timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 600);

            var defaultMaxRows = _configuration.GetSection("SqlNotebook").GetValue("DefaultMaxRows", 10_000);
            var maxRows = request.MaxRows ?? defaultMaxRows;
            maxRows = Math.Clamp(maxRows, 1, 1_000_000);

            var queryToRun = request.Query?.Trim() ?? "";
            NotebookCellExecutionResultInfo result;
            if (request.CatalogNodeId.HasValue)
            {
                result = await _nodeQueryExecutor.ExecuteAsync(request.CatalogNodeId.Value, queryToRun, timeoutSeconds, maxRows, ct).ConfigureAwait(false);
            }
            else
            {
                result = await _notebookManager.ExecuteQueryAsync(queryToRun, timeoutSeconds, maxRows, ct).ConfigureAwait(false);
            }
            return Ok(result);
        }

        [HttpPost("{id:guid}/execute/export-csv")]
        [Authorize(Policy = "EditorOrAdmin")]
        public async Task<ActionResult> ExecuteExportCsv(
            Guid id,
            [FromBody] ConnectionExecutionRequest request,
            CancellationToken ct)
        {
            if (request == null)
                return BadRequest();
            if (string.IsNullOrWhiteSpace(request.Query) && !request.CatalogNodeId.HasValue)
                return BadRequest();

            var login = User.Identity?.Name;
            if (!await _accessManager.CanEditAsync(id, login, ct).ConfigureAwait(false))
                return Forbid();

            var defaultTimeoutSeconds = _configuration.GetSection("SqlNotebook").GetValue("DefaultCommandTimeoutSeconds", 30);
            var timeoutSeconds = request.CommandTimeoutSeconds ?? defaultTimeoutSeconds;
            timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 600);
            var exportMaxRows = request.MaxRows ?? 1_000_000;
            exportMaxRows = Math.Clamp(exportMaxRows, 1, 1_000_000);

            var queryToRun = request.Query?.Trim() ?? "";
            NotebookCellExecutionResultInfo result;
            if (request.CatalogNodeId.HasValue)
                result = await _nodeQueryExecutor.ExecuteAsync(request.CatalogNodeId.Value, queryToRun, timeoutSeconds, exportMaxRows, ct).ConfigureAwait(false);
            else
                result = await _notebookManager.ExecuteQueryAsync(queryToRun, timeoutSeconds, exportMaxRows, ct).ConfigureAwait(false);

            if (result.Status != NotebookCellExecutionStatusInfo.Success || result.Columns == null)
            {
                return BadRequest(result.Error ?? "Export failed.");
            }

            var csvBytes = BuildCsv(result.Columns, result.Rows ?? Array.Empty<ExecutionResultRowInfo>());
            return File(csvBytes, "text/csv", "export.csv");
        }

        private static byte[] BuildCsv(ExecutionResultColumnInfo[] columns, ExecutionResultRowInfo[] rows)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < columns.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(EscapeCsvField(columns[i].Name ?? ""));
            }
            sb.AppendLine();
            foreach (var row in rows)
            {
                var values = row.Values?.ToList() ?? new List<string>();
                for (var i = 0; i < columns.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(EscapeCsvField(i < values.Count ? (values[i] ?? "") : ""));
                }
                sb.AppendLine();
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsvField(string value)
        {
            if (value == null) return "\"\"";
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            return value;
        }

        [HttpGet("{id:guid}/access")]
        public async Task<ActionResult<NotebookAccessResponse>> GetAccess(Guid id, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            var entries = await _accessManager.GetAccessAsync(id, login, ct).ConfigureAwait(false);
            return Ok(new NotebookAccessResponse { Entries = entries.ToList() });
        }

        [HttpPut("{id:guid}/access")]
        public async Task<ActionResult> SetAccess(Guid id, [FromBody] SetNotebookAccessRequest request, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            try
            {
                await _accessManager.SetAccessAsync(id, login, request?.Entries ?? new List<NotebookAccessEntryInfo>(), ct).ConfigureAwait(false);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("{id:guid}/access/{userLogin}")]
        public async Task<ActionResult> RemoveAccess(Guid id, string userLogin, CancellationToken ct)
        {
            var login = User.Identity?.Name;
            try
            {
                await _accessManager.RemoveAccessAsync(id, login, userLogin, ct).ConfigureAwait(false);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
