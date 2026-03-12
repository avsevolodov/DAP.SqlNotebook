using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.Services.AiSql;

namespace SqlNotebook.Service.IntegrationTests;

public sealed class TestAiSqlService : IAiSqlService
{
    public Task<AiSqlGenerateResult> GenerateAsync(AiSqlGenerateRequest request, CancellationToken ct)
    {
        var result = new AiSqlGenerateResult
        {
            Sql = "SELECT 1",
            Explanation = "Test SQL"
        };
        return Task.FromResult(result);
    }

    public Task<FindTablesResult> FindTablesAsync(FindTablesRequest request, CancellationToken ct)
    {
        var result = new FindTablesResult
        {
            Tables = new List<string> { "TestTable" }
        };
        return Task.FromResult(result);
    }

    public Task<AiSqlAutocompleteResult> AutocompleteAsync(AiSqlAutocompleteRequest request, CancellationToken ct)
    {
        var result = new AiSqlAutocompleteResult
        {
            Suggestion = "SELECT * FROM TestTable",
            Suggestions = new List<AiSqlSuggestionItem>
            {
                new AiSqlSuggestionItem
                {
                    Label = "Test suggestion",
                    InsertText = "SELECT * FROM TestTable"
                }
            }
        };
        return Task.FromResult(result);
    }

    public Task<SuggestChartResult> SuggestChartAsync(SuggestChartRequest request, CancellationToken ct)
    {
        var result = new SuggestChartResult
        {
            ChartType = "bar",
            Reason = "Test chart"
        };
        return Task.FromResult(result);
    }
}

