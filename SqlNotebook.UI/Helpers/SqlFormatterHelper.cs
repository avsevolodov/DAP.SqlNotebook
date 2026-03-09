namespace DAP.SqlNotebook.UI.Helpers;

using System.Linq;

/// <summary>
/// Simple SQL formatter for notebook cell display (keyword newlines, trim).
/// </summary>
public static class SqlFormatterHelper
{
    public static string Format(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql ?? string.Empty;
        var s = sql.Trim().ReplaceLineEndings(" ");
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        var repl = new[] {
            ("FROM", "\nFROM "),
            ("WHERE", "\nWHERE "),
            ("AND", "\n  AND "),
            ("OR", "\n  OR "),
            ("LEFT JOIN", "\nLEFT JOIN "),
            ("RIGHT JOIN", "\nRIGHT JOIN "),
            ("INNER JOIN", "\nINNER JOIN "),
            ("JOIN", "\nJOIN "),
            ("ON", "\n  ON "),
            ("GROUP BY", "\nGROUP BY "),
            ("ORDER BY", "\nORDER BY "),
            ("HAVING", "\nHAVING "),
            ("LIMIT", "\nLIMIT "),
            ("OFFSET", "\nOFFSET "),
            ("UNION ALL", "\nUNION ALL "),
            ("UNION", "\nUNION "),
            ("VALUES", "\nVALUES "),
            ("SET", "\nSET "),
        };
        foreach (var (keyword, replacement) in repl)
        {
            s = System.Text.RegularExpressions.Regex.Replace(
                s, $@"\b{System.Text.RegularExpressions.Regex.Escape(keyword)}\b", replacement,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return string.Join("\n", s.Split('\n').Select(line => line.Trim())).Replace("\n\n", "\n").Trim();
    }
}
