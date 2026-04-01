using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Integration.Auth;

public sealed class AuthContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthContractTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
    }

    [Fact]
    public async Task GetAuthMe_WhenAnonymous_ReturnsJsonUnauthorizedError()
    {
        var response = await _client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.AuthenticationRequired, payload.Error);
    }

    [Fact]
    public async Task StartTwitchLogin_ReturnsRedirectWithLocationHeader()
    {
        var response = await _client.GetAsync("/auth/twitch/login");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https", response.Headers.Location!.Scheme);
        Assert.Equal("id.twitch.tv", response.Headers.Location.Host);
        Assert.StartsWith("/oauth2/authorize", response.Headers.Location.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleTwitchCallback_WithoutCode_RedirectsToFrontendErrorRoute()
    {
        var response = await _client.GetAsync("/auth/twitch/callback?state=test-state");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https", response.Headers.Location!.Scheme);
        Assert.Equal("example.com", response.Headers.Location.Host);
        Assert.Equal("/auth/callback", response.Headers.Location.AbsolutePath);

        var query = response.Headers.Location.Query;
        Assert.Contains("status=error", query, StringComparison.Ordinal);
        Assert.Contains("reason=missing_code", query, StringComparison.Ordinal);
    }
}
