using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

namespace DAP.SqlNotebook.BL.Services.AiSql;

public sealed class AiSqlService : IAiSqlService
{
    private readonly IAiSqlBackend _backend;
    private readonly IAutocompleteCandidateService _candidateService;

    public AiSqlService(IAiSqlBackend backend, IAutocompleteCandidateService candidateService)
    {
        _backend = backend ?? throw new System.ArgumentNullException(nameof(backend));
        _candidateService = candidateService ?? throw new System.ArgumentNullException(nameof(candidateService));
    }

    public Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct)
        => _backend.GenerateAsync(request, ct);

    public Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct)
        => _backend.FindTablesAsync(request, ct);

    public async Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct)
    {
        // 1) Structural candidates (fast, in-memory)
        var cursorPos = request.CursorPosition ?? request.Sql.Length;
        var structCandidates = await _candidateService.GetCandidatesAsync(request.Sql, cursorPos, ct).ConfigureAwait(false);

        var suggestions = new List<AiSqlSuggestionItem>();
        foreach (var c in structCandidates)
        {
            suggestions.Add(new AiSqlSuggestionItem
            {
                Label = c.Text,
                InsertText = c.Text,
            });
        }

        // 2) LLM/Python autocomplete (optional, may fail independently)
        AiSqlAutocompleteResult? llmResult = null;
        try
        {
            llmResult = await _backend.AutocompleteAsync(request, ct).ConfigureAwait(false);
        }
        catch
        {
            // ignore LLM errors; structural suggestions still useful
        }

        if (llmResult?.Suggestions is { Count: > 0 })
        {
            var seen = new HashSet<string>(suggestions.Select(s => s.InsertText), StringComparer.OrdinalIgnoreCase);
            foreach (var s in llmResult.Suggestions)
            {
                if (string.IsNullOrWhiteSpace(s.InsertText))
                    continue;
                if (!seen.Add(s.InsertText))
                    continue;
                suggestions.Add(s);
            }
        }

        var first = suggestions.Count > 0 ? suggestions[0].InsertText : llmResult?.Suggestion ?? string.Empty;
        return new AiSqlAutocompleteResult
        {
            Suggestion = first ?? string.Empty,
            Suggestions = suggestions,
        };
    }

    public Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct)
        => _backend.SuggestChartAsync(request, ct);
}
