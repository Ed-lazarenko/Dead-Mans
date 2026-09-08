using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using backend.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Health;

public sealed class ObjectStorageHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;

    public ObjectStorageHealthCheck(IAmazonS3 client, IOptions<StorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await _client.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = _options.BucketName, MaxKeys = 1 },
                cancellationToken
            );

            return response.HttpStatusCode == HttpStatusCode.OK
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Object storage returned an unsuccessful response.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Object storage is unavailable.", exception);
        }
    }
}
