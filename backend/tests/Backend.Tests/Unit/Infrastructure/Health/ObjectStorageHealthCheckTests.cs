using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Infrastructure.Health;

public sealed class ObjectStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenBucketCanBeListedReturnsHealthy()
    {
        using var client = new HealthCheckAmazonS3Client(HttpStatusCode.OK);
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("deadman-test", client.LastRequest?.BucketName);
        Assert.Equal(1, client.LastRequest?.MaxKeys);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageThrowsReturnsUnhealthyWithoutThrowing()
    {
        using var client = new HealthCheckAmazonS3Client(
            new AmazonS3Exception("storage unavailable")
        );
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.IsType<AmazonS3Exception>(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStorageReturnsUnsuccessfulStatusReturnsUnhealthy()
    {
        using var client = new HealthCheckAmazonS3Client(HttpStatusCode.ServiceUnavailable);
        var healthCheck = CreateHealthCheck(client);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCallerCancelsPropagatesCancellation()
    {
        using var client = new HealthCheckAmazonS3Client(new OperationCanceledException());
        var healthCheck = CreateHealthCheck(client);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellation.Token)
        );
    }

    private static ObjectStorageHealthCheck CreateHealthCheck(IAmazonS3 client)
    {
        return new ObjectStorageHealthCheck(
            client,
            Options.Create(new StorageOptions { BucketName = "deadman-test" })
        );
    }

    private sealed class HealthCheckAmazonS3Client : AmazonS3Client
    {
        private readonly Exception? _exception;
        private readonly HttpStatusCode _statusCode;

        public HealthCheckAmazonS3Client(HttpStatusCode statusCode)
            : this(statusCode, null) { }

        public HealthCheckAmazonS3Client(Exception exception)
            : this(default, exception) { }

        private HealthCheckAmazonS3Client(HttpStatusCode statusCode, Exception? exception)
            : base(
                new AnonymousAWSCredentials(),
                new AmazonS3Config
                {
                    ServiceURL = "http://localhost:9000",
                    ForcePathStyle = true,
                }
            )
        {
            _statusCode = statusCode;
            _exception = exception;
        }

        public ListObjectsV2Request? LastRequest { get; private set; }

        public override Task<ListObjectsV2Response> ListObjectsV2Async(
            ListObjectsV2Request request,
            CancellationToken cancellationToken = default
        )
        {
            LastRequest = request;
            return _exception is null
                ? Task.FromResult(
                    new ListObjectsV2Response { HttpStatusCode = _statusCode }
                )
                : Task.FromException<ListObjectsV2Response>(_exception);
        }
    }
}
