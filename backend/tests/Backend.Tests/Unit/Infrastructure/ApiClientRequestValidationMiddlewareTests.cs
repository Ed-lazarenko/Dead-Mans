using System.Security.Claims;
using System.Text.Json;
using backend.Api.Contracts;
using backend.Api.Auth;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Unit.Infrastructure;

public sealed class ApiClientRequestValidationMiddlewareTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("TRACE")]
    public async Task InvokeAsync_RejectsCookieAuthenticatedUnsafeApiRequestWithoutClientHeader(
        string method
    )
    {
        var nextCalled = false;
        var middleware = new ApiClientRequestValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(method, "/api/game/registration/teams");

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        Assert.NotNull(response);
        Assert.Equal(AppMessages.Client.ApiClientHeaderRequired, response.Error);
        Assert.Equal(AppMessages.ErrorCodes.ApiClientHeaderRequired, response.Code);
    }

    [Fact]
    public async Task InvokeAsync_AllowsCookieAuthenticatedApiMutationWithClientHeader()
    {
        var nextCalled = false;
        var middleware = new ApiClientRequestValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(HttpMethods.Post, "/api/game/registration/teams");
        context.Request.Headers[AuthRequestHeaders.ApiClient] = AuthRequestHeaders.ApiClientValue;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_RejectsAmbiguousClientHeaderValues()
    {
        var nextCalled = false;
        var middleware = new ApiClientRequestValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(HttpMethods.Post, "/api/game/registration/teams");
        context.Request.Headers.Append(AuthRequestHeaders.ApiClient, AuthRequestHeaders.ApiClientValue);
        context.Request.Headers.Append(AuthRequestHeaders.ApiClient, AuthRequestHeaders.ApiClientValue);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task InvokeAsync_AllowsSafeCookieAuthenticatedApiRequestWithoutClientHeader(
        string method
    )
    {
        var nextCalled = false;
        var middleware = new ApiClientRequestValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(method, "/api/game");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotApplyCookieCsrfRuleToNonCookieAuthentication()
    {
        var nextCalled = false;
        var middleware = new ApiClientRequestValidationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/game/registration/teams";
        context.User = CreatePrincipal();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Headers.Cookie = $"{AuthCookieNames.Authentication}=test-ticket";
        context.Response.Body = new MemoryStream();
        context.User = CreatePrincipal();
        return context;
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, AuthRoleCodes.Viewer)
                ],
                "test"
            )
        );
    }
}
