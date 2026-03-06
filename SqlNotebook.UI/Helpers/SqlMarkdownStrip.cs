namespace DAP.SqlNotebook.UI.Helpers;

/// <summary>
/// Strips markdown code fences (e.g. ```sql ... ```) from LLM output so we show/insert raw SQL.
/// </summary>
public static class SqlMarkdownStrip
{
    /// <summary>
    /// Removes leading/trailing ```sql or ``` and trims. Returns the inner content for display/insert.
    /// </summary>
    public static string Strip(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;
        var s = text.Trim();
        // ```sql ... ``` or ``` ... ```
        const string sqlFence = "```sql";
        const string fence = "```";
        if (s.StartsWith(sqlFence, System.StringComparison.OrdinalIgnoreCase))
        {
            s = s.Substring(sqlFence.Length).Trim();
            if (s.EndsWith(fence, System.StringComparison.Ordinal))
                s = s.Substring(0, s.Length - fence.Length).Trim();
            return s;
        }
        if (s.StartsWith(fence, System.StringComparison.Ordinal))
        {
            s = s.Substring(fence.Length).Trim();
            var end = s.IndexOf(fence, System.StringComparison.Ordinal);
            if (end >= 0)
                s = s.Substring(0, end).Trim();
            return s;
        }
        return s;
    }
}
