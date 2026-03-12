using System;
using System.Collections.Generic;
using System.Linq;
using DAP.SqlNotebook.BL.Services.AiSql.Autocomplete;
using Xunit;

namespace DAP.SqlNotebook.BL.Tests.Autocomplete;

public sealed class CandidateGeneratorTests
{
    private static SchemaGraph BuildTestSchema()
    {
        var customers = new SchemaTable(
            name: "customers",
            description: "Customers table",
            columns: new[]
            {
                new SchemaColumn("id", "int", "Customer id"),
                new SchemaColumn("email", "nvarchar", "Customer email"),
                new SchemaColumn("created_at", "datetime", "Created at"),
            });

        return new SchemaGraph(new[] { customers }, Array.Empty<SchemaRelation>());
    }

    [Fact]
    public void ColumnCandidates_WithAlias_InSelectList()
    {
        // SQL:
        // SELECT c.|
        // FROM customers c

        var schema = BuildTestSchema();
        var context = new AutocompleteContext
        {
            Context = "SELECT_LIST",
            Prefix = "c.",
            Tables = new[] { "customers" },
            AliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["c"] = "customers",
            },
        };

        var providers = new ICandidateProvider[]
        {
            new ColumnCandidateProvider(),
            new AliasCandidateProvider(),
            new FunctionCandidateProvider(),
            new KeywordCandidateProvider(),
        };

        var generator = new CandidateGenerator(providers, maxResults: 20);

        var candidates = generator.Generate(context, schema);
        var texts = candidates.Select(c => c.Text).ToList();

        Assert.Contains("c.id", texts);
        Assert.Contains("c.email", texts);
        Assert.Contains("c.created_at", texts);

        // Ensure scoring prefers columns with prefix match.
        var email = candidates.First(c => c.Text == "c.email");
        var id = candidates.First(c => c.Text == "c.id");
        Assert.True(email.Score >= id.Score);

        Assert.True(candidates.Count <= 20);
    }
}

