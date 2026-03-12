using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class KeywordCandidateProvider : ICandidateProvider
{
    private static readonly string[] SelectKeywords = { "DISTINCT", "TOP" };
    private static readonly string[] WhereKeywords = { "AND", "OR", "IN", "EXISTS", "LIKE", "BETWEEN" };
    private static readonly string[] GroupByKeywords = { "ROLLUP" };

    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (schema is null) throw new ArgumentNullException(nameof(schema));

        string[] list = context.Context switch
        {
            "SELECT_LIST" => SelectKeywords,
            "WHERE" => WhereKeywords,
            "GROUP_BY" => GroupByKeywords,
            _ => Array.Empty<string>(),
        };

        if (list.Length == 0)
            return Array.Empty<CompletionItem>();

        var items = new List<CompletionItem>();
        foreach (var kw in list)
        {
            if (!PrefixMatcher.Matches(context.Prefix, kw))
                continue;

            items.Add(new CompletionItem
            {
                Text = kw,
                Type = "keyword",
                Display = kw,
                Table = null,
                Description = null,
            });
        }

        return items;
    }
}

