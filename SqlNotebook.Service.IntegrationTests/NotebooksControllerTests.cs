using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class NotebooksControllerTests : IntegrationTestBase
{
    public NotebooksControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Create_And_GetById_ReturnsNotebook()
    {
        var createRequest = new NotebookInfo
        {
            Id = Guid.NewGuid(),
            Cells = new System.Collections.Generic.List<NotebookCellInfo>(),
            Name = "Integration notebook"
        };

        var created = await NotebookManager.CreateNotebook(createRequest, default);

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        var loaded = await NotebookManager.GetNotebook(created.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);
        Assert.Equal("Integration notebook", loaded.Name);
    }
}

