using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using Backend.Tests.Support;

namespace Backend.Tests.Integration.Health;

public sealed class HealthContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthContractTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkPayload()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.Equal("Testing", payload.Environment);
        Assert.NotEqual(default, payload.ServerTimeUtc);
    }
}
