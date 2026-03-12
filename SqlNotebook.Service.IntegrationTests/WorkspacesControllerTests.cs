using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class WorkspacesControllerTests : IntegrationTestBase
{
    public WorkspacesControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Create_And_GetList_ReturnsCreatedWorkspace()
    {
        await WithDbContextAsync(async db =>
        {
            db.Workspaces.Add(new WorkspaceEntity
            {
                Id = Guid.NewGuid(),
                Name = "Existing workspace",
                OwnerLogin = "integration-test-user"
            });

            await Task.CompletedTask;
        });

        var model = new WorkspaceInfo
        {
            Name = "New workspace",
            Description = "Created from integration test",
            IsFolder = false
        };

        var created = await WorkspaceManager.CreateWorkspace(model, default);

        Assert.NotNull(created);
        Assert.Equal("New workspace", created.Name);

        var list = await WorkspaceManager.GetWorkspaces(default);

        Assert.NotNull(list);
        Assert.Contains(list, w => w.Name == "New workspace");
    }
}

