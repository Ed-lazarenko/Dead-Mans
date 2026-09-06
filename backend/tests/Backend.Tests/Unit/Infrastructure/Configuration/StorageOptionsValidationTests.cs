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
                static o => CorsOptions.IsValidAllowedOrigin(o.PublicBaseUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute http/https origin."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<StorageOptions>>().Value);

        Assert.Contains("absolute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StorageOptions_Rejects_missing_bucket_name()
    {
        var services = new ServiceCollection();
        services
            .AddOptions<StorageOptions>()
            .Configure(o =>
            {
                o.PublicBaseUrl = "https://minio.test.example";
                o.BucketName = string.Empty;
            })
            .ValidateDataAnnotations()
            .Validate(
                static o => CorsOptions.IsValidAllowedOrigin(o.PublicBaseUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute http/https origin."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<StorageOptions>>().Value);
    }

    [Fact]
    public void StorageOptions_Accepts_https_public_base_url()
    {
        var services = new ServiceCollection();
        services
            .AddOptions<StorageOptions>()
            .Configure(o =>
            {
                o.PublicBaseUrl = "https://minio.test.example";
                o.BucketName = "deadman-test";
            })
            .ValidateDataAnnotations()
            .Validate(
                static o => CorsOptions.IsValidAllowedOrigin(o.PublicBaseUrl),
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.PublicBaseUrl)} must be an absolute http/https origin."
            )
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;

        Assert.Equal("https://minio.test.example", options.PublicBaseUrl);
    }

    [Theory]
    [InlineData("file:///tmp/storage")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user@example.com")]
    [InlineData("https://minio.test.example?token=secret")]
    public void StorageOptions_Rejects_unsafe_or_non_origin_public_base_url(string publicBaseUrl)
    {
        var services = new ServiceCollection();
        services
            .AddOptions<StorageOptions>()
            .Configure(o =>
            {
                o.PublicBaseUrl = publicBaseUrl;
                o.BucketName = "deadman-test";
            })
            .ValidateDataAnnotations()
            .Validate(static o => CorsOptions.IsValidAllowedOrigin(o.PublicBaseUrl))
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<StorageOptions>>().Value
        );
    }
}
