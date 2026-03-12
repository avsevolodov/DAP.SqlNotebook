using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public interface IAutocompleteCandidateService
{
    Task<IReadOnlyList<CompletionItem>> GetCandidatesAsync(string sql, int cursorPosition, CancellationToken ct = default);
}

/// <summary>
/// High-level facade: builds AutocompleteContext from SQL and calls CandidateGenerator over SchemaGraph.
/// </summary>
public sealed class AutocompleteCandidateService : IAutocompleteCandidateService
{
    private readonly ISchemaGraphFactory _schemaFactory;

    public AutocompleteCandidateService(ISchemaGraphFactory schemaFactory)
    {
        _schemaFactory = schemaFactory ?? throw new ArgumentNullException(nameof(schemaFactory));
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCandidatesAsync(string sql, int cursorPosition, CancellationToken ct = default)
    {
        sql ??= string.Empty;
        cursorPosition = Math.Clamp(cursorPosition, 0, sql.Length);

        var context = BuildContext(sql, cursorPosition);
        var schema = await _schemaFactory.GetAsync(ct).ConfigureAwait(false);

        var providers = new ICandidateProvider[]
        {
            new TableCandidateProvider(),
            new ColumnCandidateProvider(),
            new AliasCandidateProvider(),
            new FunctionCandidateProvider(),
            new KeywordCandidateProvider(),
            new JoinCandidateProvider(),
        };

        var generator = new CandidateGenerator(providers, maxResults: 20);
        return generator.Generate(context, schema);
    }

    private static AutocompleteContext BuildContext(string sql, int cursorPosition)
    {
        var before = sql[..cursorPosition];
        var prefix = GetPrefix(before);
        var lowered = before.ToLowerInvariant();

        var (tables, aliases) = ExtractTablesAndAliases(sql);

        var ctx = "OTHER";

        if (IsAfter(lowered, " join "))
            ctx = "JOIN_TABLE";
        else if (IsAfter(lowered, " from "))
            ctx = "FROM_TABLE";
        else if (IsAfter(lowered, " where "))
            ctx = "WHERE";
        else if (IsAfter(lowered, " group by "))
            ctx = "GROUP_BY";
        else if (IsAfter(lowered, " order by "))
            ctx = "ORDER_BY";
        else if (IsBetweenSelectAndFrom(lowered))
            ctx = "SELECT_LIST";

        return new AutocompleteContext
        {
            Context = ctx,
            Prefix = prefix,
            Tables = tables,
            AliasMap = aliases,
        };
    }

    private static string GetPrefix(string before)
    {
        if (string.IsNullOrEmpty(before))
            return string.Empty;
        var match = Regex.Match(before, @"[\w.]+$");
        return match.Success ? match.Value : string.Empty;
    }

    private static bool IsAfter(string loweredBefore, string marker)
    {
        var idx = loweredBefore.LastIndexOf(marker, StringComparison.Ordinal);
        return idx >= 0 && idx + marker.Length <= loweredBefore.Length;
    }

    private static bool IsBetweenSelectAndFrom(string loweredBefore)
    {
        var sel = loweredBefore.LastIndexOf("select", StringComparison.Ordinal);
        if (sel < 0)
            return false;
        var from = loweredBefore.LastIndexOf(" from ", StringComparison.Ordinal);
        return from < 0 || from < sel;
    }

    private static (IReadOnlyList<string> Tables, IReadOnlyDictionary<string, string> Aliases) ExtractTablesAndAliases(string sql)
    {
        var tables = new List<string>();
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in Regex.Matches(sql, @"\b(from|join)\s+([A-Za-z0-9_\[\]]+)(?:\s+([A-Za-z0-9_\[\]]+))?", RegexOptions.IgnoreCase))
        {
            var table = m.Groups[2].Value;
            if (string.IsNullOrWhiteSpace(table))
                continue;
            if (!tables.Contains(table, StringComparer.OrdinalIgnoreCase))
                tables.Add(table);

            var alias = m.Groups[3].Value;
            if (!string.IsNullOrWhiteSpace(alias) && !aliases.ContainsKey(alias))
                aliases[alias] = table;
        }

        // Allow using table name as alias too (customers.id)
        foreach (var t in tables)
        {
            if (!aliases.ContainsKey(t))
                aliases[t] = t;
        }

        return (tables, aliases);
    }
}

