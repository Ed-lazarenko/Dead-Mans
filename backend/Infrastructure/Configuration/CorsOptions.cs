using System.ComponentModel.DataAnnotations;

namespace backend.Infrastructure.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1, ErrorMessage = "At least one allowed origin is required.")]
    public string[] AllowedOrigins { get; set; } = [];
}
