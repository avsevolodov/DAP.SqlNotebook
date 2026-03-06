using System;
using Laan.Sql.Formatter;

namespace DAP.SqlNotebook.Service.Helpers;

/// <summary>
/// Uses SqlFormatter (benlaan/sqlformat) for SQL formatting. Falls back to original on parse error.
/// </summary>
public static class LaanSqlFormat
{
    public static string Format(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql ?? string.Empty;
        try
        {
            var engine = new FormattingEngine { UseTabChar = false, IndentStep = 4 };
            return engine.Execute(sql.Trim());
        }
        catch
        {
            return sql;
        }
    }
}
