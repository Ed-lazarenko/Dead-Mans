using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using backend.Application.Abstractions;
using backend.Infrastructure.Configuration;

namespace backend.Infrastructure.Storage;

public sealed class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _client;

    public S3ObjectStorage(IAmazonS3 client)
    {
        _client = client;
    }

    public async Task PutObjectAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = content,
                AutoCloseStream = false,
                ContentType = contentType,
            },
            cancellationToken
        );
    }

    public async Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        await _client.DeleteObjectAsync(bucketName, objectKey, cancellationToken);
    }

    public async Task DeleteObjectsByPrefixAsync(
        string bucketName,
        string keyPrefix,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            throw new ArgumentException("Object key prefix is required.", nameof(keyPrefix));
        }

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = keyPrefix.TrimStart('/'),
        };

        while (true)
        {
            var listed = await _client.ListObjectsV2Async(listRequest, cancellationToken);
            if (listed.S3Objects.Count == 0)
            {
                return;
            }

            await _client.DeleteObjectsAsync(
                new DeleteObjectsRequest
                {
                    BucketName = bucketName,
                    Objects = listed.S3Objects
                        .Select(item => new KeyVersion { Key = item.Key })
                        .ToList(),
                },
                cancellationToken
            );

            if (!listed.IsTruncated)
            {
                return;
            }

            listRequest.ContinuationToken = listed.NextContinuationToken;
        }
    }

    internal static IAmazonS3 CreateClient(StorageOptions options)
    {
        if (!options.HasCompleteCredentials())
        {
            throw new InvalidOperationException(
                "Storage credentials are not configured. Set Storage:AccessKey/SecretKey or MINIO_ROOT_USER/MINIO_ROOT_PASSWORD."
            );
        }

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = options.GetServiceUrl().TrimEnd('/'),
            ForcePathStyle = true,
        };
        return new AmazonS3Client(credentials, config);
    }
}
