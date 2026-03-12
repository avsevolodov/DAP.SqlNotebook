using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.Contract;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DAP.SqlNotebook.Service.Controllers
{
    [ApiController]
    [Route(ApiRoutes.AiAssist)]
    public class AiAssistController : ControllerBase
    {
        private readonly IAiAssistMessageRepository _messages;
        private readonly IAiAssistSessionRepository _sessions;
        private readonly IAiSqlService _aiSql;
        private readonly IMapper _mapper;

        public AiAssistController(IAiAssistMessageRepository messages, IAiAssistSessionRepository sessions, IAiSqlService aiSql, IMapper mapper)
        {
            _messages = messages ?? throw new ArgumentNullException(nameof(messages));
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _aiSql = aiSql ?? throw new ArgumentNullException(nameof(aiSql));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<AiAssistSessionInfo>>> GetSessions([FromQuery] Guid? notebookId, CancellationToken ct)
        {
            var userLogin = User.Identity?.Name ?? "";
            var list = notebookId.HasValue && notebookId.Value != default
                ? await _sessions.GetByUserAndNotebookAsync(userLogin, notebookId, ct).ConfigureAwait(false)
                : await _sessions.GetByUserLoginAsync(userLogin, ct).ConfigureAwait(false);
            return Ok(list.Select(s => _mapper.Map<AiAssistSessionInfo>(s)).ToList());
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<AiAssistSessionInfo>> CreateSession([FromBody] AiAssistSessionInfo? model, CancellationToken ct)
        {
            var userLogin = User.Identity?.Name ?? "";
            var title = model?.Title?.Trim() ?? "New chat";
            var entity = new AiAssistSessionEntity
            {
                Id = Guid.NewGuid(),
                UserLogin = userLogin,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                NotebookId = model?.NotebookId,
            };
            await _sessions.CreateAsync(entity, ct).ConfigureAwait(false);
            return Ok(_mapper.Map<AiAssistSessionInfo>(entity));
        }

        [HttpGet("messages")]
        public async Task<ActionResult<List<AiAssistMessageInfo>>> GetMessages([FromQuery] Guid? sessionId, CancellationToken ct)
        {
            var userLogin = User.Identity?.Name ?? "";
            IReadOnlyList<AiAssistMessageEntity> list;
            if (sessionId.HasValue && sessionId.Value != default)
                list = await _messages.GetBySessionIdAsync(sessionId.Value, ct).ConfigureAwait(false);
            else
                list = await _messages.GetByUserLoginAsync(userLogin, ct).ConfigureAwait(false);
            return Ok(list.Select(m => _mapper.Map<AiAssistMessageInfo>(m)).ToList());
        }

        [HttpPost("send")]
        public async Task<ActionResult<AiAssistSendResponseInfo>> Send([FromBody] AiAssistSendRequestInfo request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content is required.");

            var userLogin = User.Identity?.Name ?? "";
            var skill = string.IsNullOrWhiteSpace(request.Skill) ? "GenerateSql" : request.Skill.Trim();
            var isFindTables = string.Equals(skill, "FindTables", StringComparison.OrdinalIgnoreCase);
            var hasNotebook = request.NotebookId != default;

            Guid? sessionId = request.SessionId;
            if (!sessionId.HasValue || sessionId.Value == default)
            {
                var newSession = new AiAssistSessionEntity
                {
                    Id = Guid.NewGuid(),
                    UserLogin = userLogin,
                    Title = "New chat",
                    CreatedAt = DateTime.UtcNow,
                    NotebookId = hasNotebook ? request.NotebookId : null,
                };
                await _sessions.CreateAsync(newSession, ct).ConfigureAwait(false);
                sessionId = newSession.Id;
            }

            var userMessage = new AiAssistMessageEntity
            {
                Id = Guid.NewGuid(),
                NotebookId = hasNotebook ? request.NotebookId : null,
                UserLogin = userLogin,
                SessionId = sessionId,
                Content = request.Content.Trim(),
                Role = 0,
                CreatedAt = DateTime.UtcNow,
            };
            await _messages.CreateAsync(userMessage, ct).ConfigureAwait(false);

            string assistantContent;
            string? sql = null;
            string? explanation = null;

            if (isFindTables)
            {
                var findResult = await _aiSql.FindTablesAsync(new FindTablesRequest { Description = request.Content.Trim() }, ct).ConfigureAwait(false);
                assistantContent = findResult.Tables != null && findResult.Tables.Count > 0
                    ? "Tables: " + string.Join(", ", findResult.Tables)
                    : "No tables found for this description.";
            }
            else
            {
                var chatHistory = request.ChatHistory?.Select(t => new AiSqlChatTurn { Role = t.Role, Content = t.Content }).ToList();
                var generateResult = await _aiSql.GenerateAsync(new AiSqlGenerateRequest
                {
                    Prompt = request.Content.Trim(),
                    SqlContext = request.SqlContext,
                    Entities = request.Entities,
                    DatabaseName = request.DatabaseName,
                    CatalogNodeId = request.CatalogNodeId,
                    ChatHistory = chatHistory,
                }, ct).ConfigureAwait(false);
                var rawSql = generateResult.Sql ?? string.Empty;
                assistantContent = !string.IsNullOrWhiteSpace(rawSql)
                    ? SqlMarkdownStrip.Strip(rawSql)
                    : (generateResult.Explanation ?? "No SQL generated.");
                sql = assistantContent;
                explanation = generateResult.Explanation;
            }

            AiAssistMessageEntity? savedAssistantMessage = null;
            var assistantMessage = new AiAssistMessageEntity
            {
                Id = Guid.NewGuid(),
                NotebookId = hasNotebook ? request.NotebookId : null,
                UserLogin = userLogin,
                SessionId = sessionId,
                Content = assistantContent,
                Role = 1,
                CreatedAt = DateTime.UtcNow,
            };
            await _messages.CreateAsync(assistantMessage, ct).ConfigureAwait(false);
            savedAssistantMessage = assistantMessage;

            var responseMessage = savedAssistantMessage != null
                ? _mapper.Map<AiAssistMessageInfo>(savedAssistantMessage)
                : new AiAssistMessageInfo
                {
                    Id = Guid.Empty,
                    NotebookId = request.NotebookId,
                    Content = assistantContent,
                    Role = 1,
                    CreatedAt = DateTime.UtcNow,
                };

            return Ok(new AiAssistSendResponseInfo
            {
                Message = responseMessage,
                Sql = sql ?? string.Empty,
                Explanation = explanation,
                SessionId = sessionId,
            });
        }
    }
}
