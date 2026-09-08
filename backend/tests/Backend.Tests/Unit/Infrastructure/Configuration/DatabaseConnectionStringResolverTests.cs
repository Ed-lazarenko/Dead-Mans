using backend.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Backend.Tests.Unit.Infrastructure.Configuration;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void ResolveAndValidate_UsesDatabaseUrlAndPreservesSupportedOptions()
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [ConfigurationKeys.DatabaseUrlEnvironmentVariable] =
                    "postgresql://app:p%40ss@db.example.com:5433/deadmans?sslmode=VerifyFull&pooling=false&maximum%20pool%20size=25"
            }
        );
        var environment = CreateEnvironment(Environments.Production);

        var result = DatabaseConnectionStringResolver.ResolveAndValidate(
            configuration,
            environment
        );

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("db.example.com", parsed.Host);
        Assert.Equal(5433, parsed.Port);
        Assert.Equal("deadmans", parsed.Database);
        Assert.Equal("app", parsed.Username);
        Assert.Equal("p@ss", parsed.Password);
        Assert.Equal(SslMode.VerifyFull, parsed.SslMode);
        Assert.False(parsed.Pooling);
        Assert.Equal(25, parsed.MaxPoolSize);
    }

    [Fact]
    public void ResolveAndValidate_WhenTestingAllowsExternalDatabaseToBeReplaced()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>());
        var environment = CreateEnvironment("Testing");

        var result = DatabaseConnectionStringResolver.ResolveAndValidate(
            configuration,
            environment
        );

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Development", "Database connection is required")]
    [InlineData("Production", "Database connection is required")]
    public void ResolveAndValidate_WhenConnectionIsMissingFailsOutsideTesting(
        string environmentName,
        string expectedMessage
    )
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>());
        var environment = CreateEnvironment(environmentName);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionStringResolver.ResolveAndValidate(configuration, environment)
        );

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Disable")]
    [InlineData("Allow")]
    [InlineData("Prefer")]
    [InlineData("Require")]
    [InlineData("VerifyCA")]
    public void ResolveAndValidate_WhenProductionTransportDoesNotVerifyHostFails(string sslMode)
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Host=db.example.com;Database=deadmans;Username=app;SSL Mode={sslMode}"
            }
        );
        var environment = CreateEnvironment(Environments.Production);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionStringResolver.ResolveAndValidate(configuration, environment)
        );

        Assert.Contains("SSL Mode=VerifyFull", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration CreateConfiguration(
        IReadOnlyDictionary<string, string?> values
    )
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestHostEnvironment { EnvironmentName = environmentName };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
