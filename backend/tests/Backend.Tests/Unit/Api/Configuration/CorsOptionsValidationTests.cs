using backend.Api.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Api.Configuration;

public sealed class CorsOptionsValidationTests
{
    [Fact]
    public void CorsOptions_Rejects_origin_with_path()
    {
        var services = new ServiceCollection();
        services
            .AddOptions<CorsOptions>()
            .Configure(options => options.AllowedOrigins = ["https://example.com/app"])
            .ValidateDataAnnotations()
            .Validate(
                static options => options.GetNormalizedAllowedOrigins().Length > 0,
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain at least one non-empty origin."
            )
            .Validate(
                static options => options.AllowedOrigins.All(CorsOptions.IsValidAllowedOrigin),
                $"{CorsOptions.SectionName}:{nameof(CorsOptions.AllowedOrigins)} must contain absolute http/https origins without paths, query strings, fragments, or user info."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<CorsOptions>>().Value);

        Assert.Contains("absolute http/https origins", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CorsOptions_Normalizes_trimmed_and_duplicate_origins()
    {
        var options = new CorsOptions
        {
            AllowedOrigins =
            [
                " https://example.com/ ",
                "https://example.com",
                "http://localhost:5180/"
            ]
        };

        var normalized = options.GetNormalizedAllowedOrigins();

        Assert.Equal(["https://example.com", "http://localhost:5180"], normalized);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com", false)]
    [InlineData("https://example.com/path", false)]
    public void IsHttpsOrigin_RequiresSecureOriginWithoutPath(string origin, bool expected)
    {
        Assert.Equal(expected, CorsOptions.IsHttpsOrigin(origin));
    }
}
