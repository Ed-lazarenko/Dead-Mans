using backend.Application.Configuration;
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
                static o => HttpOriginValidator.IsValid(o.PublicBaseUrl),
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
                static o => HttpOriginValidator.IsValid(o.PublicBaseUrl),
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
                static o => HttpOriginValidator.IsValid(o.PublicBaseUrl),
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
            .Validate(static o => HttpOriginValidator.IsValid(o.PublicBaseUrl))
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<StorageOptions>>().Value
        );
    }

    [Theory]
    [InlineData("deadman-media", true)]
    [InlineData("media.archive", true)]
    [InlineData("ABCD", false)]
    [InlineData("ab", false)]
    [InlineData("-invalid", false)]
    [InlineData("invalid-", false)]
    [InlineData("invalid..name", false)]
    [InlineData("127.0.0.1", false)]
    public void IsValidBucketName_EnforcesPortableS3Names(string bucketName, bool expected)
    {
        Assert.Equal(expected, StorageOptions.IsValidBucketName(bucketName));
    }

    [Fact]
    public void GetServiceUrl_UsesExplicitInternalEndpointWhenConfigured()
    {
        var options = new StorageOptions
        {
            PublicBaseUrl = "https://cdn.example.com",
            ServiceUrl = "https://s3.internal.example.com"
        };

        Assert.Equal("https://s3.internal.example.com", options.GetServiceUrl());
    }

    [Theory]
    [InlineData("access", "secret", true)]
    [InlineData("access", "", false)]
    [InlineData("", "secret", false)]
    public void HasCompleteCredentials_RequiresBothValues(
        string accessKey,
        string secretKey,
        bool expected
    )
    {
        var options = new StorageOptions { AccessKey = accessKey, SecretKey = secretKey };

        Assert.Equal(expected, options.HasCompleteCredentials());
    }
}
