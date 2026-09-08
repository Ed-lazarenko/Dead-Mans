using backend.Api.Http;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Unit.Infrastructure;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ForApiRequest_AddsSecurityHeadersIncludingCsp()
    {
        var context = CreateHttpContext("/api/game");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("camera=(), geolocation=(), microphone=()", context.Response.Headers["Permissions-Policy"]);
        Assert.Equal("same-origin", context.Response.Headers["Cross-Origin-Opener-Policy"]);
        Assert.Equal("none", context.Response.Headers["X-Permitted-Cross-Domain-Policies"]);
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.Response.Headers.Pragma);
        Assert.Equal(
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'",
            context.Response.Headers["Content-Security-Policy"]
        );
    }

    [Fact]
    public async Task InvokeAsync_ForSwaggerRequest_SkipsCspHeader()
    {
        var context = CreateHttpContext("/swagger/index.html");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public async Task InvokeAsync_ForAuthRequest_DisablesCaching()
    {
        var context = CreateHttpContext("/auth/me");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.Response.Headers.Pragma);
    }

    [Fact]
    public async Task InvokeAsync_ForSignalRNegotiation_DisablesCaching()
    {
        var context = CreateHttpContext("/hubs/game-board/negotiate");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.Response.Headers.Pragma);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }
}
