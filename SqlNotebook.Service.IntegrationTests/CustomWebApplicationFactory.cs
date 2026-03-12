using System.Linq;
using System.Net.Http;
using DAP.SqlNotebook.BL.DataAccess;
using DAP.SqlNotebook.BL.Services.AiSql;
using DAP.SqlNotebook.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace SqlNotebook.Service.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureTestServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SqlNotebookDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextFactoryDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<SqlNotebookDbContext>));

            if (dbContextFactoryDescriptor != null)
            {
                services.Remove(dbContextFactoryDescriptor);
            }

            services.AddDbContext<SqlNotebookDbContext>(options =>
            {
                options.UseInMemoryDatabase("SqlNotebookTests");
            });

            var aiSqlDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAiSqlService));

            if (aiSqlDescriptor != null)
            {
                services.Remove(aiSqlDescriptor);
            }

            services.AddSingleton<IAiSqlService, TestAiSqlService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("EditorOrAdmin", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });
            });
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "test-user");
        return client;
    }
}

