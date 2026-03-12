using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class CatalogControllerTests : IntegrationTestBase
{
    public CatalogControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateEntity_Then_GetTables_ReturnsCreatedEntity()
    {
        var request = new CreateDbEntityInfo
        {
            Name = "Products",
            DisplayName = "Products",
            SchemaName = "dbo",
            Description = "Products table"
        };

        var created = await CatalogManager.CreateEntityAsync(request, default);

        Assert.NotNull(created);
        Assert.Equal("Products", created.Name);

        var list = await CatalogManager.GetEntitiesAsync(null, default);

        Assert.NotNull(list);
        Assert.Contains(list, e => e.Name == "Products");
    }
}

