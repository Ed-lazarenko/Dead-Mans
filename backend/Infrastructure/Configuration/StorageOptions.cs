using System.ComponentModel.DataAnnotations;

namespace backend.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Public base URL for object storage (e.g. Minio) used to build media URLs for clients.
    /// Must be an absolute URL; validated when options are bound (<c>AddDeadMansInfrastructure</c>).
    /// </summary>
    [Required]
    public string PublicBaseUrl { get; set; } = string.Empty;
}
