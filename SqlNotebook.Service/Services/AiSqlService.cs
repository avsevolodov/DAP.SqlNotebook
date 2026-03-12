using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.AiSql;
using Microsoft.Extensions.Configuration;

namespace DAP.SqlNotebook.Service.Services;

public sealed class AiSqlHttpBackend : IAiSqlBackend
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AiSqlHttpBackend(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct)
    {
        var baseUrl = _configuration["AiSqlService:BaseUrl"] ?? "http://localhost:8000/generate-sql";
        using var client = _httpClientFactory.CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(baseUrl, new
            {
                prompt = request.Prompt,
                entity = request.Entity,
                entities = request.Entities,
                sql_context = request.SqlContext,
                database_name = request.DatabaseName,
                catalog_node_id = request.CatalogNodeId.HasValue ? request.CatalogNodeId.Value.ToString("N") : null,
                chat_history = request.ChatHistory == null ? null : request.ChatHistory.Select(t => new { role = t.Role, content = t.Content }).ToList(),
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new AiSqlGenerateResult { Sql = string.Empty, Explanation = $"Failed to reach AI SQL service: {ex.Message}" };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new AiSqlGenerateResult { Sql = string.Empty, Explanation = $"AI SQL service error: {errorText}" };
            }
            var body = await response.Content.ReadFromJsonAsync<AiSqlGenerateResult>(SnakeCaseJsonOptions.Options, ct).ConfigureAwait(false);
            return body ?? new AiSqlGenerateResult { Sql = string.Empty, Explanation = "Empty response." };
        }
    }

    public async Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct)
    {
        var baseUrl = _configuration["AiSqlService:BaseUrl"] ?? "http://localhost:8000/generate-sql";
        var aiBase = baseUrl.Replace("/generate-sql", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        var url = aiBase + "/find-tables";
        var client = _httpClientFactory.CreateClient();
        try
        {
            using var response = await client.PostAsJsonAsync(url, new { description = request.Description }, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new FindTablesResult { Tables = new List<string> { $"Error: {errorText}" } };
            }
            var body = await response.Content.ReadFromJsonAsync<FindTablesResult>(SnakeCaseJsonOptions.Options, ct).ConfigureAwait(false);
            return body ?? new FindTablesResult();
        }
        catch (Exception)
        {
            return new FindTablesResult { Tables = new List<string> { "Service unavailable." } };
        }
    }

    private static readonly TimeSpan AutocompleteHttpTimeout = TimeSpan.FromSeconds(5);

    public async Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct)
    {
        var url = _configuration["AiSqlService:AutocompleteUrl"] ?? "http://localhost:8000/autocomplete-sql";
        var client = _httpClientFactory.CreateClient();
        client.Timeout = AutocompleteHttpTimeout;
        HttpResponseMessage response;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(AutocompleteHttpTimeout);
            response = await client.PostAsJsonAsync(
                url,
                new
                {
                    sql = request.Sql,
                    entities = request.Entities,
                    cursor_position = request.CursorPosition,
                },
                timeoutCts.Token
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;
            return new AiSqlAutocompleteResult
            {
                Suggestion = "",
                Suggestions = new List<AiSqlSuggestionItem> { new() { Label = "Unavailable", InsertText = $"-- {ex.Message}" } }
            };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return new AiSqlAutocompleteResult
                {
                    Suggestion = "",
                    Suggestions = new List<AiSqlSuggestionItem> { new() { Label = "Error", InsertText = $"-- {errorText}" } }
                };
            }
            var body = await response.Content.ReadFromJsonAsync<AiSqlAutocompleteResult>(SnakeCaseJsonOptions.Options, ct).ConfigureAwait(false);
            if (body == null)
                return new AiSqlAutocompleteResult { Suggestions = new List<AiSqlSuggestionItem>() };
            if (body.Suggestions == null && !string.IsNullOrEmpty(body.Suggestion))
                body.Suggestions = new List<AiSqlSuggestionItem> { new() { Label = body.Suggestion, InsertText = body.Suggestion } };
            return body;
        }
    }

    public async Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct)
    {
        var baseUrl = _configuration["AiSqlService:BaseUrl"] ?? "http://localhost:8000/generate-sql";
        var aiBase = baseUrl.Replace("/generate-sql", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        var url = aiBase + "/suggest-chart";
        var client = _httpClientFactory.CreateClient();
        try
        {
            using var response = await client.PostAsJsonAsync(url, new { columns = request.Columns, rows = request.Rows }, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var heuristic = ChartSuggestHeuristic.Suggest(request.Columns ?? new List<string>(), request.Rows ?? new List<List<string>>());
                return new SuggestChartResult { ChartType = heuristic.ChartType, Reason = $"AI error, using heuristic. {errorText}" };
            }
            var body = await response.Content.ReadFromJsonAsync<SuggestChartResult>(SnakeCaseJsonOptions.Options, ct).ConfigureAwait(false);
            if (body != null && !string.IsNullOrEmpty(body.ChartType))
                return body;
            return ChartSuggestHeuristic.Suggest(request.Columns ?? new List<string>(), request.Rows ?? new List<List<string>>());
        }
        catch (Exception)
        {
            var heuristic = ChartSuggestHeuristic.Suggest(request.Columns ?? new List<string>(), request.Rows ?? new List<List<string>>());
            return new SuggestChartResult { ChartType = heuristic.ChartType, Reason = "AI unavailable, suggested from data structure." };
        }
    }
}
