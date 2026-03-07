using AutoMapper;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Services;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.BL.Services.Notebook;
using DAP.SqlNotebook.Service.Mapper;
using DAP.SqlNotebook.Service.Services;
using DAP.SqlNotebook.Service.Services.Database;
using DAP.SqlNotebook.Service.Services.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAP.SqlNotebook.Service.Configuration;

internal static class IoCConfigurator
{

    public static void AddManagementServices(IServiceCollection services, ServiceSettings settings)
    {
        services.AddDbContext<SqlNotebookDbContext>((sp, opts) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var conn = config.GetConnectionString("SqlNotebook")
                       ?? "Server=(localdb)\\mssqllocaldb;Database=SqlNotebook;TrustServerCertificate=True;";
            opts.UseSqlServer(conn);
        });

        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<INotebookFavoritesRepository, NotebookFavoritesRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IAiAssistMessageRepository, AiAssistMessageRepository>();
        services.AddScoped<IAiAssistSessionRepository, AiAssistSessionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddSingleton<IDbProviderStrategy, MssqlProviderStrategy>();
        services.AddSingleton<IDbProviderStrategy, ClickHouseProviderStrategy>();
        services.AddSingleton<IDbProviderStrategyFactory, DbProviderStrategyFactory>();
        services.AddDataProtection();
        services.AddScoped<IDataSourcePasswordProtector, DataSourcePasswordProtector>();
        services.AddScoped<IConnectionHealthService, ConnectionHealthService>();
        services.AddScoped<ISchemaImportService, SchemaImportService>();
        services.AddScoped<IKafkaCatalogService, KafkaCatalogService>();
        services.AddScoped<IKafkaMessageReader, KafkaCatalogService>();
        services.AddScoped<IQueryExecutor, DbContextQueryExecutor>();
        services.AddScoped<IQueryExecutionService, QueryExecutionService>();
        services.AddScoped<INodeQueryExecutorService, NodeQueryExecutorService>();
        services.AddScoped<IAiSqlBackend, AiSqlHttpBackend>();
        services.AddScoped<IAiSqlService, DAP.SqlNotebook.BL.Services.AiSql.AiSqlService>();
        services.AddScoped<INotebookManager, NotebookManager>();
        
        services.AddSingleton<IMapper>(_ => new AutoMapper.Mapper(new MapperConfiguration(conf =>
        {
            conf.AllowNullCollections = true;

            conf.AddProfile<ManagementBLProfile>();
        })));
    }
}

public class ServiceSettings
{
}