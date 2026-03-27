using backend.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Infrastructure.Configuration;

public sealed class StorageOptionsValidationTests
{
    [Fact]
    public void StorageOptions_Rejects_non_absolute_public_base_url()
    {
        var services = new ServiceCollection();
        services
            .AddOptions<StorageOptions>()
            .Configure(o => o.PublicBaseUrl = "relative-is-not-absolute")
            .ValidateDataAnnotations()
            .Validate(
                static o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute URL."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<StorageOptions>>().Value);

        Assert.Contains("absolute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StorageOptions_Accepts_https_public_base_url()
    {
        var services = new ServiceCollection();
        services
            .AddOptions<StorageOptions>()
            .Configure(o => o.PublicBaseUrl = "https://minio.test.example")
            .ValidateDataAnnotations()
            .Validate(
                static o => Uri.TryCreate(o.PublicBaseUrl, UriKind.Absolute, out _),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute URL."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;

        Assert.Equal("https://minio.test.example", options.PublicBaseUrl);
    }
}
