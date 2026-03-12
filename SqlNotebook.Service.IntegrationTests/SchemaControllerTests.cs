using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class SchemaControllerTests : IntegrationTestBase
{
    public SchemaControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetSchema_ReturnsEntitiesFromCatalog()
    {
        var entityId = Guid.NewGuid();

        await WithDbContextAsync(async db =>
        {
            db.DbEntities.Add(new DbEntityDescription
            {
                Id = entityId,
                Name = "Customers",
                DisplayName = "Customers",
                Description = "Customers table"
            });

            db.DbFields.Add(new DbFieldDescription
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                Name = "Id",
                DataType = "int",
                IsPrimaryKey = true,
                IsNullable = false
            });

            await Task.CompletedTask;
        });

        var schema = await SchemaClient.GetSchemaAsync(null, default);

        Assert.NotNull(schema);
        Assert.Contains(schema!.Entities, e => e.Name == "Customers");
    }

    [Fact]
    public async Task Autocomplete_ReturnsSuggestions_ForPrefix()
    {
        var entityId = Guid.NewGuid();

        await WithDbContextAsync(async db =>
        {
            db.DbEntities.Add(new DbEntityDescription
            {
                Id = entityId,
                Name = "Orders",
                DisplayName = "Orders",
                Description = "Orders table"
            });

            await Task.CompletedTask;
        });

        var request = new SchemaAutocompleteRequest
        {
            Sql = "SELECT * FROM Ord",
            Line = 1,
            Column = 18
        };

        var items = await SchemaClient.AutocompleteAsync(request, default);

        Assert.NotNull(items);
        Assert.Contains(items!, x => x.Label == "Orders");
    }
}

