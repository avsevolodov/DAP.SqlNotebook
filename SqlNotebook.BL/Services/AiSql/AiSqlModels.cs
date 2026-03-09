using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.Services.AiSql;

/// <summary>
/// Gateway to external AI SQL service (HTTP or other). Implemented in the host (e.g. Service).
/// </summary>
public interface IAiSqlBackend
{
    Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct);
    Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct);
    Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct);
    Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct);
}

/// <summary>
/// Business logic service for AI SQL. Delegates to external backend.
/// </summary>
public interface IAiSqlService
{
    Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct);

    Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct);

    Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct);

    Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct);
}

// BL models (no suffix per project conventions)

public sealed class AiSqlGenerateRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public List<string>? Entities { get; set; }
    public string? SqlContext { get; set; }
    public string? DatabaseName { get; set; }
    /// <summary>When set, AI SQL service fetches schema for this source only (better generation).</summary>
    public Guid? CatalogNodeId { get; set; }
    /// <summary>Previous chat turns (user question + assistant answer) to add to prompt for context.</summary>
    public List<AiSqlChatTurn>? ChatHistory { get; set; }
}

public sealed class AiSqlChatTurn
{
    public int Role { get; set; }
    public string? Content { get; set; }
}

public sealed class AiSqlGenerateResult
{
    public string Sql { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}

public sealed class FindTablesRequest
{
    public string Description { get; set; } = string.Empty;
}

public sealed class FindTablesResult
{
    public List<string> Tables { get; set; } = new();
}

public sealed class AiSqlAutocompleteRequest
{
    public string Sql { get; set; } = string.Empty;
    public List<string>? Entities { get; set; }
}

public sealed class AiSqlSuggestionItem
{
    public string Label { get; set; } = string.Empty;
    public string InsertText { get; set; } = string.Empty;
}

public sealed class AiSqlAutocompleteResult
{
    public string Suggestion { get; set; } = string.Empty;
    public List<AiSqlSuggestionItem>? Suggestions { get; set; }
}

public sealed class SuggestChartRequest
{
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public sealed class SuggestChartResult
{
    public string ChartType { get; set; } = "table";
    public string Reason { get; set; } = "";
}
