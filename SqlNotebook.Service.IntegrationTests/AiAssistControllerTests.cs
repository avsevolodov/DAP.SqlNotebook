using System;
using System.Threading.Tasks;
using DAP.SqlNotebook.Contract.Entities;
using DAP.SqlNotebook.Service.Client.V1;
using Xunit;

namespace SqlNotebook.Service.IntegrationTests;

public class AiAssistControllerTests : IntegrationTestBase
{
    public AiAssistControllerTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateSession_And_SendMessage_ReturnsResponse()
    {
        var sessionRequest = new AiAssistSessionInfo
        {
            Title = "Test session"
        };

        var session = await AiAssistClient.CreateSessionAsync(sessionRequest, default);

        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.Id);

        var sendRequest = new AiAssistSendRequestInfo
        {
            Content = "test prompt",
            SessionId = session.Id
        };

        var body = await AiAssistClient.SendAsync(sendRequest, default);

        Assert.NotNull(body);
        Assert.Equal(session.Id, body.SessionId);
        Assert.False(string.IsNullOrEmpty(body.Sql));
    }
}

