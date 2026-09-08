using System.Net;
using backend.Api.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Api;

public sealed class RateLimitingServiceCollectionExtensionsTests
{
    [Fact]
    public async Task FixedWindowLimiter_ReplenishesPermitsAfterConfiguredWindow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:Reads:PermitLimit"] = "1",
                    ["RateLimiting:Reads:WindowSeconds"] = "1"
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDeadMansRateLimiting(configuration, new TestHostEnvironment());

        await using var serviceProvider = services.BuildServiceProvider();
        var limiter = serviceProvider
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value.GlobalLimiter;
        Assert.NotNull(limiter);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/game";
        context.Request.Method = HttpMethods.Get;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        using var firstLease = await limiter.AcquireAsync(context);
        using var exhaustedLease = await limiter.AcquireAsync(context);
        Assert.True(firstLease.IsAcquired);
        Assert.False(exhaustedLease.IsAcquired);

        await Task.Delay(TimeSpan.FromMilliseconds(1_200));

        using var replenishedLease = await limiter.AcquireAsync(context);
        Assert.True(replenishedLease.IsAcquired);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Staging;

        public string ApplicationName { get; set; } = "Backend.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
