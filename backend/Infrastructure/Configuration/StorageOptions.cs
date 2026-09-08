using System.ComponentModel.DataAnnotations;

namespace backend.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string? ServiceUrl { get; set; }

    [Required]
    public string BucketName { get; set; } = string.Empty;

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    public string GetServiceUrl()
    {
        return string.IsNullOrWhiteSpace(ServiceUrl) ? PublicBaseUrl : ServiceUrl;
    }

    public bool HasCompleteCredentials()
    {
        return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);
    }

    public static bool IsValidBucketName(string? bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return false;
        }

        var value = bucketName.Trim();
        if (value.Length is < 3 or > 63
            || !char.IsAsciiLetterOrDigit(value[0])
            || !char.IsAsciiLetterOrDigit(value[^1])
            || value.Contains("..", StringComparison.Ordinal)
            || value.Contains(".-", StringComparison.Ordinal)
            || value.Contains("-.", StringComparison.Ordinal)
            || System.Net.IPAddress.TryParse(value, out _))
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterLower(character)
            || char.IsAsciiDigit(character)
            || character is '-' or '.'
        );
    }
}
