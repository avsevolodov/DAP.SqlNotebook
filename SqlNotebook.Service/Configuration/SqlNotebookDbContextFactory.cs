using System.IO;
using DAP.SqlNotebook.BL.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DAP.SqlNotebook.Service.Configuration
{
    /// <summary>
    /// Design-time factory for EF Core migrations (e.g. dotnet ef migrations add --project SqlNotebook.Service).
    /// </summary>
    public class SqlNotebookDbContextFactory : IDesignTimeDbContextFactory<SqlNotebookDbContext>
    {
        public SqlNotebookDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("SqlNotebook")
                ?? "Server=(localdb)\\mssqllocaldb;Database=SqlNotebook;TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<SqlNotebookDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new SqlNotebookDbContext(optionsBuilder.Options);
        }
    }
}
