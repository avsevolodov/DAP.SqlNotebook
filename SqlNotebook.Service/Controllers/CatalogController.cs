using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Models;
using DAP.SqlNotebook.BL.Services;
using DAP.SqlNotebook.Contract;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Services;
using DAP.SqlNotebook.Service.Services.Kafka;
using Microsoft.AspNetCore.Mvc;
using DbEntityInfo = DAP.SqlNotebook.Contract.Entities.DbEntityInfo;
using DbFieldInfo = DAP.SqlNotebook.Contract.Entities.DbFieldInfo;

namespace DAP.SqlNotebook.Service.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Catalog)]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogRepository _repository;
        private readonly IMapper _mapper;
        private readonly IConnectionHealthService _connectionHealth;
        private readonly ISchemaImportService _schemaImport;
        private readonly IDataSourcePasswordProtector _passwordProtector;
        private readonly IKafkaCatalogService _kafkaCatalog;

        public CatalogController(ICatalogRepository repository, IMapper mapper, IConnectionHealthService connectionHealth, ISchemaImportService schemaImport, IDataSourcePasswordProtector passwordProtector, IKafkaCatalogService kafkaCatalog)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _connectionHealth = connectionHealth ?? throw new ArgumentNullException(nameof(connectionHealth));
            _schemaImport = schemaImport ?? throw new ArgumentNullException(nameof(schemaImport));
            _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
            _kafkaCatalog = kafkaCatalog ?? throw new ArgumentNullException(nameof(kafkaCatalog));
        }

        /// <summary>
        /// Get child nodes for the catalog tree. Pass no parentId or empty for root nodes.
        /// Tree is loaded asynchronously: call again with parentId when user expands a node.
        /// For Kafka source nodes, topics are loaded from the broker on first expand.
        /// </summary>
        [HttpGet("nodes")]
        public async Task<ActionResult<List<CatalogNodeInfo>>> GetNodes(
            [FromQuery] Guid? parentId,
            CancellationToken ct)
        {
            if (parentId.HasValue)
            {
                var parent = await _repository.GetNodeByIdAsync(parentId.Value, ct).ConfigureAwait(false);
                if (parent != null && string.Equals(parent.Provider, "Kafka", StringComparison.OrdinalIgnoreCase) && string.Equals(parent.Type, "Database", StringComparison.OrdinalIgnoreCase))
                    await _kafkaCatalog.EnsureTopicsLoadedAsync(parentId.Value, ct).ConfigureAwait(false);
            }
            var nodes = await _repository.GetNodesAsync(parentId, ct).ConfigureAwait(false);
            var list = nodes.Select(n => _mapper.Map<CatalogNodeInfo>(n)).ToList();
            return Ok(list);
        }

        /// <summary>
        /// List all catalog nodes of type Database (for schema/RAG: separate endpoint for DB list).
        /// </summary>
        [HttpGet("databases")]
        public async Task<ActionResult<List<CatalogNodeInfo>>> GetDatabases(CancellationToken ct)
        {
            var nodes = await _repository.GetDatabaseNodesAsync(ct).ConfigureAwait(false);
            return Ok(nodes.Select(n => _mapper.Map<CatalogNodeInfo>(n)).ToList());
        }

        /// <summary>
        /// List all tables (entities) with id, name, displayName, description. Use entities/{id}/fields for columns.
        /// </summary>
        [HttpGet("tables")]
        public async Task<ActionResult<List<DbEntityInfo>>> GetTables(CancellationToken ct)
        {
            var list = await _repository.GetEntitiesAsync(ct).ConfigureAwait(false);
            return Ok(list.Select(e => _mapper.Map<DbEntityInfo>(e)).ToList());
        }

        [HttpGet("nodes/{id:guid}")]
        public async Task<ActionResult<CatalogNodeInfo>> GetNode(Guid id, CancellationToken ct)
        {
            var node = await _repository.GetNodeByIdAsync(id, ct).ConfigureAwait(false);
            if (node == null) return NotFound();
            return Ok(_mapper.Map<CatalogNodeInfo>(node));
        }

        [HttpPut("nodes/{id:guid}")]
        public async Task<ActionResult<CatalogNodeInfo>> UpdateNode(Guid id, [FromBody] CatalogNodeUpdateInfo model, CancellationToken ct)
        {
            if (model == null) return BadRequest();
            string? passwordEncrypted = null;
            if (model.Password != null)
                passwordEncrypted = _passwordProtector.Protect(model.Password);
            var update = new UpdateCatalogNodeParams
            {
                Name = model.Name,
                Description = model.Description,
                Owner = model.Owner,
                Provider = model.Provider,
                ConnectionInfo = model.ConnectionInfo,
                DatabaseName = model.DatabaseName,
                AuthType = model.AuthType,
                Login = model.Login,
                PasswordEncrypted = passwordEncrypted,
                ConsumerGroupPrefix = model.ConsumerGroupPrefix,
                ConsumerGroupAutoGenerate = model.ConsumerGroupAutoGenerate,
            };
            var node = await _repository.UpdateNodeAsync(id, update, ct).ConfigureAwait(false);
            if (node == null) return NotFound();
            return Ok(_mapper.Map<CatalogNodeInfo>(node));
        }

        [HttpDelete("nodes/{id:guid}")]
        public async Task<ActionResult> DeleteNode(Guid id, CancellationToken ct)
        {
            var deleted = await _repository.DeleteNodeAsync(id, ct).ConfigureAwait(false);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("nodes/{id:guid}/import-structure")]
        public async Task<ActionResult<SchemaImportResultInfo>> ImportStructure(Guid id, CancellationToken ct)
        {
            var result = await _schemaImport.ImportStructureAsync(id, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result.Error))
                return BadRequest(new SchemaImportResultInfo { TablesCount = 0, FieldsCount = 0, Error = result.Error });
            return Ok(new SchemaImportResultInfo { TablesCount = result.TablesCount, FieldsCount = result.FieldsCount });
        }

        [HttpGet("nodes/{id:guid}/connection-status")]
        public async Task<ActionResult<ConnectionHealthInfo>> GetConnectionStatus(Guid id, CancellationToken ct)
        {
            var result = await _connectionHealth.CheckNodeAsync(id, ct).ConfigureAwait(false);
            var info = new ConnectionHealthInfo
            {
                Status = (ConnectionStatusInfo)result.Status,
                Message = result.Message,
            };
            return Ok(info);
        }

        [HttpGet("entity/{entityId:guid}/select-text")]
        public async Task<ActionResult<string>> GetEntitySelectText(Guid entityId, [FromQuery] int? top, CancellationToken ct)
        {
            var n = top ?? 10;
            var text = await _repository.GetEntitySelectTextAsync(entityId, n, ct).ConfigureAwait(false);
            if (text == null)
                return NotFound();
            return Ok(text);
        }

        [HttpPost("nodes")]
        public async Task<ActionResult<CatalogNodeInfo>> CreateNode(
            [FromBody] CatalogNodeCreateInfo model,
            CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Name is required.");
            string? passwordEncrypted = null;
            if (model.Password != null && !string.IsNullOrEmpty(model.Password))
                passwordEncrypted = _passwordProtector.Protect(model.Password);
            var create = new CreateCatalogNodeParams
            {
                ParentId = model.ParentId,
                Type = model.Type,
                Name = model.Name,
                Description = model.Description,
                Owner = model.Owner,
                Provider = model.Provider,
                ConnectionInfo = model.ConnectionInfo,
                DatabaseName = model.DatabaseName,
                AuthType = model.AuthType,
                Login = model.Login,
                PasswordEncrypted = passwordEncrypted,
                ConsumerGroupPrefix = model.ConsumerGroupPrefix,
                ConsumerGroupAutoGenerate = model.ConsumerGroupAutoGenerate,
                EntityId = model.EntityId,
            };
            var node = await _repository.CreateNodeAsync(create, ct).ConfigureAwait(false);
            return Ok(_mapper.Map<CatalogNodeInfo>(node));
        }

        [HttpGet("entities")]
        public async Task<ActionResult<List<DbEntityInfo>>> GetEntities(
            [FromQuery] Guid? nodeId,
            [FromQuery] int? offset,
            [FromQuery] int? count,
            CancellationToken ct)
        {
            if (nodeId.HasValue && (offset.HasValue || count.HasValue))
            {
                var skip = Math.Max(0, offset ?? 0);
                var take = Math.Clamp(count ?? 20, 1, 200);
                var (items, totalCount) = await _repository.GetEntitiesBySourceNodePagedAsync(nodeId.Value, skip, take, ct).ConfigureAwait(false);
                var contractItems = items.Select(e => _mapper.Map<DbEntityInfo>(e)).ToList();
                return Ok(new { Items = contractItems, TotalCount = totalCount });
            }
            var list = nodeId.HasValue
                ? await _repository.GetEntitiesBySourceNodeAsync(nodeId.Value, ct).ConfigureAwait(false)
                : await _repository.GetEntitiesAsync(ct).ConfigureAwait(false);
            return Ok(list.Select(e => _mapper.Map<DbEntityInfo>(e)).ToList());
        }

        [HttpGet("entities/{entityId:guid}")]
        public async Task<ActionResult<DbEntityInfo>> GetEntity(Guid entityId, CancellationToken ct)
        {
            var entity = await _repository.GetEntityByIdAsync(entityId, ct).ConfigureAwait(false);
            if (entity == null) return NotFound();
            return Ok(_mapper.Map<DbEntityInfo>(entity));
        }

        [HttpPost("entities")]
        public async Task<ActionResult<DbEntityInfo>> CreateEntity([FromBody] CreateDbEntityInfo model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name)) return BadRequest("Name is required.");
            var create = new CreateDbEntityParams { Name = model.Name, DisplayName = model.DisplayName, SchemaName = model.SchemaName, Description = model.Description };
            var entity = await _repository.CreateEntityAsync(create, ct).ConfigureAwait(false);
            return Ok(_mapper.Map<DbEntityInfo>(entity));
        }

        [HttpPut("entities/{entityId:guid}")]
        public async Task<ActionResult<DbEntityInfo>> UpdateEntity(Guid entityId, [FromBody] UpdateDbEntityInfo model, CancellationToken ct)
        {
            if (model == null) return BadRequest();
            var update = new UpdateDbEntityParams { Name = model.Name, DisplayName = model.DisplayName, SchemaName = model.SchemaName, Description = model.Description };
            var entity = await _repository.UpdateEntityAsync(entityId, update, ct).ConfigureAwait(false);
            if (entity == null) return NotFound();
            return Ok(_mapper.Map<DbEntityInfo>(entity));
        }

        [HttpDelete("entities/{entityId:guid}")]
        public async Task<ActionResult> DeleteEntity(Guid entityId, CancellationToken ct)
        {
            var deleted = await _repository.DeleteEntityAsync(entityId, ct).ConfigureAwait(false);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("entities/{entityId:guid}/fields")]
        public async Task<ActionResult<List<DbFieldInfo>>> GetFields(Guid entityId, CancellationToken ct)
        {
            var list = await _repository.GetFieldsAsync(entityId, ct).ConfigureAwait(false);
            return Ok(list.Select(f => _mapper.Map<DbFieldInfo>(f)).ToList());
        }

        /// <summary>
        /// Get relations for a table (entity). Returns empty list if no relations stored.
        /// </summary>
        [HttpGet("entities/{entityId:guid}/relations")]
        public async Task<ActionResult<List<SchemaRelationDto>>> GetEntityRelations(Guid entityId, CancellationToken ct)
        {
            return Ok(new List<SchemaRelationDto>());
        }

        [HttpPost("fields")]
        public async Task<ActionResult<DbFieldInfo>> CreateField([FromBody] CreateDbFieldInfo model, CancellationToken ct)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name)) return BadRequest("Name is required.");
            var create = new CreateDbFieldParams
            {
                EntityId = model.EntityId,
                Name = model.Name,
                DataType = model.DataType,
                IsNullable = model.IsNullable,
                IsPrimaryKey = model.IsPrimaryKey,
                Description = model.Description,
            };
            var field = await _repository.CreateFieldAsync(create, ct).ConfigureAwait(false);
            return Ok(_mapper.Map<DbFieldInfo>(field));
        }

        [HttpPut("fields/{fieldId:guid}")]
        public async Task<ActionResult<DbFieldInfo>> UpdateField(Guid fieldId, [FromBody] UpdateDbFieldInfo model, CancellationToken ct)
        {
            if (model == null) return BadRequest();
            var update = new UpdateDbFieldParams
            {
                Name = model.Name,
                DataType = model.DataType,
                IsNullable = model.IsNullable,
                IsPrimaryKey = model.IsPrimaryKey,
                Description = model.Description,
            };
            var field = await _repository.UpdateFieldAsync(fieldId, update, ct).ConfigureAwait(false);
            if (field == null) return NotFound();
            return Ok(_mapper.Map<DbFieldInfo>(field));
        }

        [HttpDelete("fields/{fieldId:guid}")]
        public async Task<ActionResult> DeleteField(Guid fieldId, CancellationToken ct)
        {
            var deleted = await _repository.DeleteFieldAsync(fieldId, ct).ConfigureAwait(false);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
