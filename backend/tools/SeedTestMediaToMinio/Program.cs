using System.Net;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

static Dictionary<string, string> LoadDotEnv(string path)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!File.Exists(path))
    {
        return result;
    }

    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var idx = line.IndexOf('=');
        if (idx <= 0)
        {
            continue;
        }

        var key = line.Substring(0, idx).Trim();
        var value = line[(idx + 1)..].Trim().Trim('"');
        result[key] = value;
    }

    return result;
}

static string GetArg(string[] args, string name, string defaultValue)
{
    var prefix = $"--{name}=";
    foreach (var a in args)
    {
        if (a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return a[prefix.Length..];
        }
    }

    return defaultValue;
}

static async Task<bool> TryHeadObjectAsync(IAmazonS3 s3, string bucket, string key)
{
    try
    {
        await s3.GetObjectMetadataAsync(bucket, key);
        return true;
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        return false;
    }
    catch (AmazonS3Exception ex)
        when (ex.StatusCode == HttpStatusCode.MovedPermanently || ex.StatusCode == HttpStatusCode.Found)
    {
        // Some S3-compatible endpoints (including misconfigured local MinIO URLs)
        // may redirect HEAD requests. Treat it as "not sure => attempt upload".
        return false;
    }
}

static string GuessMimeTypeFromExtension(string extension)
{
    return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
        ? "image/png"
        : "application/octet-stream";
}

static async Task UploadWithRetriesAsync(
    IAmazonS3 s3,
    string bucket,
    string key,
    string filePath,
    string contentType,
    int maxRetries,
    int baseDelayMs
)
{
    Exception? lastException = null;

    for (var attempt = 1; attempt <= maxRetries; attempt += 1)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);

            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            await s3.PutObjectAsync(request);
            return;
        }
        catch (AmazonS3Exception ex) when (
            attempt < maxRetries &&
            (ex.StatusCode == HttpStatusCode.RequestTimeout
                || ex.StatusCode == HttpStatusCode.InternalServerError
                || ex.StatusCode == HttpStatusCode.ServiceUnavailable
                || ex.StatusCode == HttpStatusCode.BadGateway))
        {
            lastException = ex;
            var delayMs = (int)(baseDelayMs * Math.Pow(2, attempt - 1));
            delayMs = Math.Min(delayMs, 8000);
            await Task.Delay(delayMs);
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            lastException = ex;
            var delayMs = (int)(baseDelayMs * Math.Pow(2, attempt - 1));
            delayMs = Math.Min(delayMs, 8000);
            await Task.Delay(delayMs);
        }
    }

    throw lastException ?? new Exception($"Failed to upload '{filePath}' to '{bucket}/{key}'.");
}

static void PrintUsage()
{
    Console.WriteLine(
        "Usage:" +
        "\n  dotnet run --project backend/tools/SeedTestMediaToMinio/SeedTestMediaToMinio.csproj " +
        "-- --sourceDir=<path> " +
        "--bucket=deadman " +
        "--endpoint=http://localhost:9000 " +
        "--group=elements " +
        "--gameId=<guid> " +
        "--maxRetries=5");
}

var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (cliArgs.Length > 0 && cliArgs.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase)))
{
    PrintUsage();
    return;
}

var defaultGameId = Guid.Parse("c6c6a0da-0bd1-4f0b-bb2f-9a4c9c8b7f6a");
var defaultGroup = "elements";
var defaultBucket = "deadman";
var defaultEndpoint = "http://localhost:9000";

var repoRoot = Directory.GetCurrentDirectory();

var dotenvPath = Path.Combine(repoRoot, ".env");
var dotenv = LoadDotEnv(dotenvPath);

string minioUser =
    GetArg(cliArgs, "accessKey", dotenv.TryGetValue("MINIO_ROOT_USER", out var u) ? u : "");
string minioPassword =
    GetArg(cliArgs, "secretKey", dotenv.TryGetValue("MINIO_ROOT_PASSWORD", out var p) ? p : "");

if (string.IsNullOrWhiteSpace(minioUser) || string.IsNullOrWhiteSpace(minioPassword))
{
    Console.WriteLine("Missing MINIO credentials. Provide --accessKey/--secretKey or ensure .env has MINIO_ROOT_USER/MINIO_ROOT_PASSWORD.");
    return;
}

var bucket = GetArg(cliArgs, "bucket", defaultBucket);
var endpoint = GetArg(cliArgs, "endpoint", defaultEndpoint);
var group = GetArg(cliArgs, "group", defaultGroup);
var sourceDir = GetArg(
    cliArgs,
    "sourceDir",
    Path.Combine(repoRoot, "assets", "test-media", "loadout", "elements"));

if (!Directory.Exists(sourceDir))
{
    Console.WriteLine($"Source dir not found: {sourceDir}");
    return;
}

var gameId = GetArg(cliArgs, "gameId", defaultGameId.ToString());
var maxRetries = int.TryParse(GetArg(cliArgs, "maxRetries", "5"), out var r) ? r : 5;
var baseDelayMs = int.TryParse(GetArg(cliArgs, "baseDelayMs", "500"), out var d) ? d : 500;

var keyPrefix = $"games/{gameId}/{group}/";

var credentials = new BasicAWSCredentials(minioUser, minioPassword);
var s3Config = new AmazonS3Config
{
    ServiceURL = endpoint,
    ForcePathStyle = true,
    RegionEndpoint = RegionEndpoint.USEast1
};

using var s3 = new AmazonS3Client(credentials, s3Config);

// Ensure bucket exists (idempotent).
try
{
    var buckets = await s3.ListBucketsAsync();
    if (!buckets.Buckets.Any(b => b.BucketName == bucket))
    {
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        Console.WriteLine($"Created bucket: {bucket}");
    }
}
catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
{
    Console.WriteLine("Forbidden while listing buckets. Bucket may already exist; continuing.");
}

var files = Directory.EnumerateFiles(sourceDir, "*.png", SearchOption.TopDirectoryOnly)
    .Select(p => new FileInfo(p))
    .OrderBy(fi => fi.Name)
    .ToArray();

Console.WriteLine($"Uploading {files.Length} png files from '{sourceDir}' to s3://{bucket}/{keyPrefix}...");

var uploaded = 0;
var skipped = 0;
foreach (var file in files)
{
    var filename = file.Name;
    var key = keyPrefix + filename;
    var contentType = GuessMimeTypeFromExtension(file.Extension);

    var exists = await TryHeadObjectAsync(s3, bucket, key);
    if (exists)
    {
        skipped += 1;
        Console.WriteLine($"SKIP  {filename}");
        continue;
    }

    Console.WriteLine($"UPLOAD {filename}");
    await UploadWithRetriesAsync(s3, bucket, key, file.FullName, contentType, maxRetries, baseDelayMs);
    uploaded += 1;
}

Console.WriteLine($"Done. Uploaded={uploaded}, Skipped={skipped}.");

