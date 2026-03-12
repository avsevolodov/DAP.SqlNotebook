using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.Service.Client.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime, IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    protected INotebookManager NotebookManager { get; private set; } = null!;
    protected ICatalogManager CatalogManager { get; private set; } = null!;
    protected IWorkspaceManager WorkspaceManager { get; private set; } = null!;
    protected INotebookFavoritesClient NotebookFavoritesClient { get; private set; } = null!;
    protected IAiAssistClient AiAssistClient { get; private set; } = null!;
    protected IAiSqlClient AiSqlClient { get; private set; } = null!;
    protected ISchemaClient SchemaClient { get; private set; } = null!;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task InitializeAsync()
    {
        // Resolve typed SDK clients instead of using raw HttpClient
        NotebookManager = _factory.Services.GetRequiredService<INotebookManager>();
        CatalogManager = _factory.Services.GetRequiredService<ICatalogManager>();
        WorkspaceManager = _factory.Services.GetRequiredService<IWorkspaceManager>();
        NotebookFavoritesClient = _factory.Services.GetRequiredService<INotebookFavoritesClient>();
        AiAssistClient = _factory.Services.GetRequiredService<IAiAssistClient>();
        AiSqlClient = _factory.Services.GetRequiredService<IAiSqlClient>();
        SchemaClient = _factory.Services.GetRequiredService<ISchemaClient>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqlNotebookDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected async Task WithDbContextAsync(Func<SqlNotebookDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqlNotebookDbContext>();
        await action(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected async Task<T> WithDbContextAsync<T>(Func<SqlNotebookDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqlNotebookDbContext>();
        var result = await action(dbContext);
        await dbContext.SaveChangesAsync();
        return result;
    }
}

