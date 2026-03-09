using System.Collections.Generic;
using DAP.SqlNotebook.BL.Services.AiSql;

namespace DAP.SqlNotebook.Service.Services;

/// <summary>
/// Heuristic fallback when AI chart suggestion is unavailable.
/// </summary>
internal static class ChartSuggestHeuristic
{
    public static SuggestChartResult Suggest(IList<string> columns, IList<List<string>> rows)
    {
        if (columns == null || columns.Count == 0)
            return new SuggestChartResult { ChartType = "table", Reason = "No columns." };
        if (rows == null || rows.Count == 0)
            return new SuggestChartResult { ChartType = "table", Reason = "No data." };
        if (columns.Count == 1 && rows.Count > 1)
            return new SuggestChartResult { ChartType = "pie", Reason = "Single series, many values." };
        if (columns.Count == 2 && rows.Count > 3)
            return new SuggestChartResult { ChartType = "line", Reason = "Two columns, time/series-like." };
        return new SuggestChartResult { ChartType = "table", Reason = "Default." };
    }
}
