using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class AutocompleteContext
{
    public string Context { get; init; } = string.Empty; // SELECT_LIST, FROM_TABLE, JOIN_TABLE, ...
    public string Prefix { get; init; } = string.Empty;
    public IReadOnlyList<string> Tables { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> AliasMap { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class CompletionItem
{
    public string Text { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // table, column, function, join, keyword, alias
    public string Display { get; init; } = string.Empty;
    public string? Table { get; init; }
    public string? Description { get; init; }
    public int Score { get; set; }
}

public interface ICandidateProvider
{
    IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema);
}

public static class PrefixMatcher
{
    public static bool Matches(string? prefix, string candidate)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return true;

        prefix = prefix!.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(candidate))
            return false;

        var value = candidate.ToLowerInvariant();
        if (value.StartsWith(prefix, StringComparison.Ordinal))
            return true;

        var compact = value.Replace("_", string.Empty);
        if (compact.StartsWith(prefix, StringComparison.Ordinal))
            return true;

        var acronym = GetAcronym(value);
        return acronym.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static string GetAcronym(string value)
    {
        var acc = new List<char>(value.Length);
        var take = true;
        foreach (var ch in value)
        {
            if (take && char.IsLetter(ch))
                acc.Add(ch);
            take = ch == '_' || char.IsUpper(ch);
        }
        return new string(acc.ToArray());
    }
}

