using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

var options = SeederOptions.From(args);
var sourceFiles = ResolveSourceFiles(options.SourceDirectory);

if (string.IsNullOrWhiteSpace(options.AccessKey) || string.IsNullOrWhiteSpace(options.SecretKey))
{
    Console.Error.WriteLine("Missing MinIO credentials. Provide --accessKey/--secretKey or set MINIO_ROOT_USER/MINIO_ROOT_PASSWORD.");
    return 1;
}

var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
var config = new AmazonS3Config
{
    ServiceURL = options.Endpoint,
    ForcePathStyle = true
};

using var client = new AmazonS3Client(credentials, config);

await EnsureBucketExistsAsync(client, options.Bucket);
await EnsurePublicReadPolicyAsync(client, options.Bucket);

foreach (var sourceFile in sourceFiles)
{
    var objectKey = $"games/{options.GameId}/{options.Group}/{sourceFile.Name}";
    await using var input = sourceFile.OpenRead();
    await client.PutObjectAsync(
        new PutObjectRequest
        {
            BucketName = options.Bucket,
            Key = objectKey,
            InputStream = input,
            AutoCloseStream = true,
            ContentType = "image/png"
        }
    );

    Console.WriteLine($"Uploaded {options.Bucket}/{objectKey}");
}

return 0;

static async Task EnsureBucketExistsAsync(IAmazonS3 client, string bucketName)
{
    try
    {
        await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = bucketName });
        return;
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        await client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
    }
}

static async Task EnsurePublicReadPolicyAsync(IAmazonS3 client, string bucketName)
{
    var policy = $$"""
    {
      "Version": "2012-10-17",
      "Statement": [
        {
          "Sid": "PublicReadGetObject",
          "Effect": "Allow",
          "Principal": "*",
          "Action": [
            "s3:GetObject"
          ],
          "Resource": [
            "arn:aws:s3:::{{bucketName}}/*"
          ]
        }
      ]
    }
    """;

    await client.PutBucketPolicyAsync(
        new PutBucketPolicyRequest
        {
            BucketName = bucketName,
            Policy = policy
        }
    );
}

static FileInfo[] ResolveSourceFiles(string sourceDirectory)
{
    var directory = new DirectoryInfo(sourceDirectory);
    if (!directory.Exists)
    {
        throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
    }

    var files = directory
        .EnumerateFiles("*.png", SearchOption.TopDirectoryOnly)
        .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (files.Length == 0)
    {
        throw new InvalidOperationException($"No PNG files found in source directory: {sourceDirectory}");
    }

    return files;
}

file sealed class SeederOptions
{
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string Endpoint { get; init; }
    public required string Bucket { get; init; }
    public required Guid GameId { get; init; }
    public required string Group { get; init; }
    public required string SourceDirectory { get; init; }

    public static SeederOptions From(string[] args)
    {
        return new SeederOptions
        {
            AccessKey = GetArg(args, "accessKey") ?? Environment.GetEnvironmentVariable("MINIO_ROOT_USER") ?? string.Empty,
            SecretKey = GetArg(args, "secretKey") ?? Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD") ?? string.Empty,
            Endpoint = GetArg(args, "endpoint") ?? "http://localhost:9000",
            Bucket = GetArg(args, "bucket") ?? "deadman",
            GameId = Guid.TryParse(GetArg(args, "gameId"), out var gameId)
                ? gameId
                : Guid.Parse("c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a"),
            Group = GetArg(args, "group") ?? "elements",
            SourceDirectory = GetArg(args, "sourceDir")
                ?? Path.Combine(AppContext.BaseDirectory, "assets", "test-game-board", "elements")
        };
    }

    private static string? GetArg(string[] args, string name)
    {
        var prefix = $"--{name}=";
        return args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            ?.Substring(prefix.Length);
    }
}
