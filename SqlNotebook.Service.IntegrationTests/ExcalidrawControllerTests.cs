using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess.Entities;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class ExcalidrawControllerTests : IntegrationTestBase
{
    public ExcalidrawControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Put_And_Get_ExcalidrawContent_Works()
    {
        var notebookId = Guid.NewGuid();
        const int cellId = 1;

        await WithDbContextAsync(async db =>
        {
            db.Notebooks.Add(new NotebookEntity
            {
                Id = notebookId,
                Name = "Excalidraw notebook",
                Cells = new List<NotebookCellEntity>
                {
                    new NotebookCellEntity
                    {
                        Id = cellId,
                        CellType = NotebookCellTypeEntity.Excalidraw,
                        Content = "{}"
                    }
                }
            });

            await Task.CompletedTask;
        });

        var request = new ExcalidrawContentRequest
        {
            Content = "{\"type\":\"excalidraw\",\"version\":1}"
        };

        // Use NotebookManager (SDK) to update notebook with new Excalidraw content
        var notebook = await NotebookManager.GetNotebook(notebookId, default);
        Assert.NotNull(notebook);
        var cell = Assert.Single(notebook!.Cells!);
        Assert.Equal(cellId, cell.Id);

        cell.Content = request.Content;
        await NotebookManager.UpdateNotebook(notebookId, notebook, default);

        var reloaded = await NotebookManager.GetNotebook(notebookId, default);
        Assert.NotNull(reloaded);
        var reloadedCell = Assert.Single(reloaded!.Cells!);
        Assert.Equal(request.Content, reloadedCell.Content);
    }
}

