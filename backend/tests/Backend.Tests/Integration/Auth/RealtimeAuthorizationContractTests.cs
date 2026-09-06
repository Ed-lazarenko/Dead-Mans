using System.Net;
using backend.Application.Abstractions.Auth;
using Backend.Tests.Support;

namespace Backend.Tests.Integration.Auth;

public sealed class RealtimeAuthorizationContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RealtimeAuthorizationContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GameBoardNegotiate_WhenAnonymous_ReturnsUnauthorizedWithoutRedirect()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/hubs/game-board/negotiate?negotiateVersion=1",
            content: null
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task GameSetupNegotiate_WhenViewer_ReturnsForbiddenWithoutRedirect()
    {
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer]
        );

        var response = await client.PostAsync(
            "/hubs/game-setup/negotiate?negotiateVersion=1",
            content: null
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
