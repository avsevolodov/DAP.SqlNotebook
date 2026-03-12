using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class JoinCandidateProvider : ICandidateProvider
{
    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (context.Context != "JOIN_TABLE")
            return Array.Empty<CompletionItem>();

        var existingTables = context.Tables ?? Array.Empty<string>();
        if (existingTables.Count == 0)
            return Array.Empty<CompletionItem>();

        // Map table name -> alias (first one wins) to generate joins that respect existing aliases.
        var aliasByTable = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in context.AliasMap)
        {
            var alias = kvp.Key;
            var tableName = kvp.Value;
            if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(tableName))
                continue;
            if (!aliasByTable.ContainsKey(tableName))
                aliasByTable[tableName] = alias;
        }

        var items = new List<CompletionItem>();

        foreach (var fromTableName in existingTables)
        {
            var fromTable = schema.TryGetTable(fromTableName);
            if (fromTable is null)
                continue;

            foreach (var rel in schema.GetOutgoingRelations(fromTableName))
            {
                var toTable = schema.TryGetTable(rel.ToTable);
                if (toTable is null)
                    continue;

                if (!PrefixMatcher.Matches(context.Prefix, toTable.Name))
                    continue;

                var toAlias = SchemaGraph.SuggestAlias(toTable.Name);
                var fromSide = aliasByTable.TryGetValue(fromTable.Name, out var fromAlias)
                    ? fromAlias
                    : fromTable.Name;

                var snippet = $"JOIN {toTable.Name} {toAlias} ON {toAlias}.{rel.ToColumn} = {fromSide}.{rel.FromColumn}";

                items.Add(new CompletionItem
                {
                    Text = snippet,
                    Type = "join",
                    Display = snippet,
                    Table = toTable.Name,
                    Description = $"Join {fromTable.Name} → {toTable.Name}",
                });
            }
        }

        return items;
    }
}

