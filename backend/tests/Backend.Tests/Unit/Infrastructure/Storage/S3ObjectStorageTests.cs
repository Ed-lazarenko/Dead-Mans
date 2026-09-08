using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using backend.Infrastructure.Storage;

namespace Backend.Tests.Unit.Infrastructure.Storage;

public sealed class S3ObjectStorageTests
{
    [Fact]
    public async Task DeleteObjectsByPrefixAsync_ListsEveryPageBeforeDeletingInS3SizedBatches()
    {
        using var client = new RecordingAmazonS3Client();
        var storage = new S3ObjectStorage(client);

        await storage.DeleteObjectsByPrefixAsync("deadman-test", "/games/game-id/");

        Assert.Equal([null, "page-2"], client.ListContinuationTokens);
        Assert.Equal(["list", "list", "delete", "delete"], client.Operations);
        Assert.Equal([1000, 1], client.DeletedBatches.Select(batch => batch.Count));
        Assert.Equal(1001, client.DeletedBatches.SelectMany(batch => batch).Distinct().Count());
        Assert.All(client.ListPrefixes, prefix => Assert.Equal("games/game-id/", prefix));
    }

    [Fact]
    public async Task DeleteObjectsByPrefixAsync_WhenTruncatedPageHasNoToken_Throws()
    {
        using var client = new RecordingAmazonS3Client(returnMissingContinuationToken: true);
        var storage = new S3ObjectStorage(client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.DeleteObjectsByPrefixAsync("deadman-test", "games/game-id/")
        );

        Assert.Contains("continuation token", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(client.DeletedBatches);
    }

    private sealed class RecordingAmazonS3Client : AmazonS3Client
    {
        private readonly bool _returnMissingContinuationToken;

        public RecordingAmazonS3Client(bool returnMissingContinuationToken = false)
            : base(
                new AnonymousAWSCredentials(),
                new AmazonS3Config
                {
                    ServiceURL = "http://localhost:9000",
                    ForcePathStyle = true,
                }
            )
        {
            _returnMissingContinuationToken = returnMissingContinuationToken;
        }

        public List<string?> ListContinuationTokens { get; } = [];

        public List<string> ListPrefixes { get; } = [];

        public List<IReadOnlyList<string>> DeletedBatches { get; } = [];

        public List<string> Operations { get; } = [];

        public override Task<ListObjectsV2Response> ListObjectsV2Async(
            ListObjectsV2Request request,
            CancellationToken cancellationToken = default
        )
        {
            Operations.Add("list");
            ListContinuationTokens.Add(request.ContinuationToken);
            ListPrefixes.Add(request.Prefix);

            if (request.ContinuationToken is null)
            {
                return Task.FromResult(
                    new ListObjectsV2Response
                    {
                        IsTruncated = true,
                        NextContinuationToken = _returnMissingContinuationToken ? null : "page-2",
                        S3Objects = Enumerable
                            .Range(0, 1000)
                            .Select(index => new S3Object { Key = $"games/game-id/{index}" })
                            .ToList(),
                    }
                );
            }

            return Task.FromResult(
                new ListObjectsV2Response
                {
                    IsTruncated = false,
                    S3Objects = [new S3Object { Key = "games/game-id/1000" }],
                }
            );
        }

        public override Task<DeleteObjectsResponse> DeleteObjectsAsync(
            DeleteObjectsRequest request,
            CancellationToken cancellationToken = default
        )
        {
            Operations.Add("delete");
            DeletedBatches.Add(request.Objects.Select(item => item.Key).ToArray());
            return Task.FromResult(new DeleteObjectsResponse());
        }
    }
}
