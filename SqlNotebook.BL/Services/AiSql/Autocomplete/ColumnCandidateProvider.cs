using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class ColumnCandidateProvider : ICandidateProvider
{
    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.Context is not ("SELECT_LIST" or "WHERE" or "GROUP_BY" or "ORDER_BY"))
            return Array.Empty<CompletionItem>();

        var items = new List<CompletionItem>();

        // 1) Columns via aliases: c.id, c.email
        foreach (var kvp in context.AliasMap)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;
            var table = schema.TryGetTable(tableName);
            if (table is null) continue;

            foreach (var col in table.Columns)
            {
                var text = $"{alias}.{col.Name}";
                if (!PrefixMatcher.Matches(context.Prefix, text))
                    continue;

                items.Add(new CompletionItem
                {
                    Text = text,
                    Type = "column",
                    Display = text,
                    Table = table.Name,
                    Description = col.Description,
                });
            }
        }

        // 2) Without aliases: customers.id, customers.email
        if (context.AliasMap.Count == 0 && context.Tables.Count > 0)
        {
            foreach (var tableName in context.Tables)
            {
                var table = schema.TryGetTable(tableName);
                if (table is null) continue;

                foreach (var col in table.Columns)
                {
                    var text = $"{table.Name}.{col.Name}";
                    if (!PrefixMatcher.Matches(context.Prefix, text))
                        continue;

                    items.Add(new CompletionItem
                    {
                        Text = text,
                        Type = "column",
                        Display = text,
                        Table = table.Name,
                        Description = col.Description,
                    });
                }
            }
        }

        return items;
    }
}

