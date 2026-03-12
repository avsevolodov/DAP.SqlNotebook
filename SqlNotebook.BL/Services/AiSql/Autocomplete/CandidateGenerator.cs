using System;
using System.Collections.Generic;
using System.Linq;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class CandidateGenerator
{
    private readonly IReadOnlyList<ICandidateProvider> _providers;
    private readonly int _maxResults;

    public CandidateGenerator(IReadOnlyList<ICandidateProvider> providers, int maxResults = 20)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _maxResults = maxResults;
    }

    public IReadOnlyList<CompletionItem> Generate(AutocompleteContext context, SchemaGraph schema)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (schema is null) throw new ArgumentNullException(nameof(schema));

        var all = new List<CompletionItem>();

        foreach (var provider in _providers)
        {
            all.AddRange(provider.GetCandidates(context, schema));
        }

        if (all.Count == 0)
            return Array.Empty<CompletionItem>();

        // Deduplicate by Text
        var byText = new Dictionary<string, CompletionItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in all)
        {
            if (string.IsNullOrWhiteSpace(item.Text))
                continue;
            if (byText.ContainsKey(item.Text))
                continue;
            byText[item.Text] = item;
        }

        var deduped = byText.Values.ToList();

        // Scoring
        foreach (var item in deduped)
        {
            item.Score = 0;
            if (PrefixMatcher.Matches(context.Prefix, item.Text))
                item.Score += 50;

            if (item.Type == "column"
                && !string.IsNullOrWhiteSpace(item.Table)
                && context.Tables.Contains(item.Table!, StringComparer.OrdinalIgnoreCase))
            {
                item.Score += 30;
            }

            if (item.Type == "function")
                item.Score += 20;
            if (item.Type == "keyword")
                item.Score += 10;
        }

        return deduped
            .OrderByDescending(i => i.Score)
            .ThenBy(i => i.Text, StringComparer.OrdinalIgnoreCase)
            .Take(_maxResults)
            .ToList();
    }
}

