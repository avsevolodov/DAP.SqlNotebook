using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers
{
    [ApiController]
    [Route("api/v1/ai/sql")]
    public class AiSqlController : ControllerBase
    {
        private readonly IAiSqlService _aiSqlService;
        private readonly IMapper _mapper;

        public AiSqlController(IAiSqlService aiSqlService, IMapper mapper)
        {
            _aiSqlService = aiSqlService ?? throw new System.ArgumentNullException(nameof(aiSqlService));
            _mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        public async Task<ActionResult<AiSqlResponseInfo>> Generate([FromBody] AiSqlRequestInfo request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest();

            var blRequest = _mapper.Map<AiSqlGenerateRequest>(request);
            var blResult = await _aiSqlService.GenerateAsync(blRequest, ct).ConfigureAwait(false);
            var response = _mapper.Map<AiSqlResponseInfo>(blResult);
            return Ok(response);
        }

        [HttpPost("find-tables")]
        public async Task<ActionResult<FindTablesResponseInfo>> FindTables([FromBody] FindTablesRequestInfo request, CancellationToken ct)
        {
            if (request == null)
                return BadRequest();

            var blRequest = _mapper.Map<FindTablesRequest>(request);
            var blResult = await _aiSqlService.FindTablesAsync(blRequest, ct).ConfigureAwait(false);
            var response = _mapper.Map<FindTablesResponseInfo>(blResult);
            return Ok(response);
        }

        [HttpPost("autocomplete")]
        public async Task<ActionResult<AiSqlAutocompleteResponseInfo>> Autocomplete([FromBody] AiSqlAutocompleteRequestInfo request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Sql))
                return BadRequest();

            var blRequest = _mapper.Map<AiSqlAutocompleteRequest>(request);
            var blResult = await _aiSqlService.AutocompleteAsync(blRequest, ct).ConfigureAwait(false);
            var response = _mapper.Map<AiSqlAutocompleteResponseInfo>(blResult);
            return Ok(response);
        }

        [HttpPost("suggest-chart")]
        public async Task<ActionResult<SuggestChartResponseInfo>> SuggestChart([FromBody] SuggestChartRequestInfo request, CancellationToken ct)
        {
            if (request == null || request.Columns == null || request.Rows == null)
                return BadRequest();

            var blRequest = _mapper.Map<SuggestChartRequest>(request);
            var blResult = await _aiSqlService.SuggestChartAsync(blRequest, ct).ConfigureAwait(false);
            var response = _mapper.Map<SuggestChartResponseInfo>(blResult);
            return Ok(response);
        }

        /// <summary>Format SQL using SqlFormatter (benlaan/sqlformat). Returns original on parse error.</summary>
        [HttpPost("format")]
        public ActionResult<FormatSqlResponseInfo> FormatSql([FromBody] FormatSqlRequestInfo request)
        {
            var sql = request?.Sql ?? string.Empty;
            var formatted = LaanSqlFormat.Format(sql);
            return Ok(new FormatSqlResponseInfo { Formatted = formatted });
        }
    }
}
