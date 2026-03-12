using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class AiSqlControllerTests : IntegrationTestBase
{
    public AiSqlControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Generate_ReturnsResponse_FromTestService()
    {
        var request = new AiSqlRequestInfo
        {
            Prompt = "select something"
        };

        var response = await AiSqlClient.GenerateAsync(request, default);

        Assert.NotNull(response);
        Assert.Equal("SELECT 1", response!.Sql);
    }

    [Fact]
    public async Task FindTables_ReturnsTables_FromTestService()
    {
        var request = new FindTablesRequestInfo
        {
            Description = "some description"
        };

        var response = await AiSqlClient.FindTablesAsync(request, default);

        Assert.NotNull(response);
        Assert.Contains("TestTable", response!.Tables);
    }
}

