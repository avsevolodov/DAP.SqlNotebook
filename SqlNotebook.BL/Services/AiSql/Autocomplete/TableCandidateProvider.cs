using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class TableCandidateProvider : ICandidateProvider
{
    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.Context is not ("FROM_TABLE" or "JOIN_TABLE"))
            return Array.Empty<CompletionItem>();

        var used = new HashSet<string>(
            context.Tables ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        var items = new List<CompletionItem>();
        foreach (var table in schema.Tables)
        {
            if (used.Contains(table.Name))
                continue;
            if (!PrefixMatcher.Matches(context.Prefix, table.Name))
                continue;

            items.Add(new CompletionItem
            {
                Text = table.Name,
                Type = "table",
                Display = table.Name,
                Table = table.Name,
                Description = table.Description,
            });
        }

        return items;
    }
}

