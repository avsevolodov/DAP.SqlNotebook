using System;
using System.Collections.Generic;

namespace DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;

public sealed class FunctionCandidateProvider : ICandidateProvider
{
    private static readonly string[] Functions =
    {
        "COUNT",
        "SUM",
        "AVG",
        "MIN",
        "MAX",
        "DATEADD",
        "DATEDIFF",
        "CAST",
        "CONVERT",
    };

    public IReadOnlyList<CompletionItem> GetCandidates(AutocompleteContext context, SchemaGraph schema)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (schema is null) throw new ArgumentNullException(nameof(schema));

        if (context.Context is not ("SELECT_LIST" or "WHERE"))
            return Array.Empty<CompletionItem>();

        var items = new List<CompletionItem>();
        foreach (var f in Functions)
        {
            if (!PrefixMatcher.Matches(context.Prefix, f))
                continue;

            items.Add(new CompletionItem
            {
                Text = f + "(", // user will type closing paren
                Type = "function",
                Display = f,
                Table = null,
                Description = null,
            });
        }

        return items;
    }
}

