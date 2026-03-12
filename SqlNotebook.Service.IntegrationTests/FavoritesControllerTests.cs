using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class FavoritesControllerTests : IntegrationTestBase
{
    public FavoritesControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateFolder_And_AddNotebook_ToFavorites_Works()
    {
        var notebookId = Guid.NewGuid();

        await WithDbContextAsync(async db =>
        {
            db.Notebooks.Add(new NotebookEntity
            {
                Id = notebookId,
                Name = "Fav notebook"
            });

            await Task.CompletedTask;
        });

        var folder = await NotebookFavoritesClient.CreateFolderAsync("My favorites", null, default);

        Assert.NotNull(folder);
        Assert.Equal("My favorites", folder.Name);

        await NotebookFavoritesClient.AddNotebookToFavoritesAsync(notebookId, folder.Id, default);

        var list = await NotebookFavoritesClient.GetFavoriteNotebooksAsync(default);

        Assert.NotNull(list);
        Assert.Contains(list, x => x.NotebookId == notebookId && x.FolderId == folder.Id);
    }
}

