using System.ComponentModel.DataAnnotations;

namespace backend.Application.Configuration;

public sealed class MediaStorageSettings
{
    public const string SectionName = "Storage";

    [Required]
    public string PublicBaseUrl { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    [Required]
    public string GamesPrefix { get; set; } = string.Empty;

    [Required]
    public string CardsGroup { get; set; } = string.Empty;

    public static bool IsValidObjectKeyPrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return false;
        }

        var value = prefix.Trim('/');
        if (value.Length is 0 or > 256)
        {
            return false;
        }

        return value
            .Split('/', StringSplitOptions.None)
            .All(segment =>
                segment.Length is > 0 and <= 64
                && segment is not "." and not ".."
                && segment.All(character =>
                    char.IsAsciiLetterOrDigit(character)
                    || character is '-' or '_' or '.'
                )
            );
    }
}
