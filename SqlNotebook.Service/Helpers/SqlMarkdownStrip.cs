using System;

namespace DAP.SqlNotebook.Service.Helpers;

/// <summary>
/// Strips markdown code fences (e.g. ```sql ... ```) from LLM output so we store/return raw SQL.
/// </summary>
public static class SqlMarkdownStrip
{
    public static string Strip(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;
        var s = text.Trim();
        const string sqlFence = "```sql";
        const string fence = "```";
        if (s.StartsWith(sqlFence, StringComparison.OrdinalIgnoreCase))
        {
            s = s.Substring(sqlFence.Length).Trim();
            if (s.EndsWith(fence, StringComparison.Ordinal))
                s = s.Substring(0, s.Length - fence.Length).Trim();
            return s;
        }
        if (s.StartsWith(fence, StringComparison.Ordinal))
        {
            s = s.Substring(fence.Length).Trim();
            var end = s.IndexOf(fence, StringComparison.Ordinal);
            if (end >= 0)
                s = s.Substring(0, end).Trim();
            return s;
        }
        return s;
    }
}
