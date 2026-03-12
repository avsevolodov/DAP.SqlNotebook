using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class AliasCandidateProvider : ICandidateProvider
{
    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (schema is null) throw new ArgumentNullException(nameof(schema));

        if (context.Context is not ("SELECT_LIST" or "WHERE" or "ORDER_BY"))
            return Array.Empty<CompletionItem>();

        var items = new List<CompletionItem>();
        foreach (var alias in context.AliasMap.Keys)
        {
            var text = alias + ".";
            if (!PrefixMatcher.Matches(context.Prefix, text))
                continue;

            items.Add(new CompletionItem
            {
                Text = text,
                Type = "alias",
                Display = text,
                Table = null,
                Description = null,
            });
        }

        return items;
    }
}

