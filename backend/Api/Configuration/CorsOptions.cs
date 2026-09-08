using System.ComponentModel.DataAnnotations;
using backend.Application.Configuration;

namespace backend.Api.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1, ErrorMessage = "At least one allowed origin is required.")]
    public string[] AllowedOrigins { get; set; } = [];

    public string[] GetNormalizedAllowedOrigins()
    {
        return AllowedOrigins
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool IsValidAllowedOrigin(string? origin)
    {
        return HttpOriginValidator.IsValid(origin);
    }

    public static bool IsHttpsOrigin(string? origin)
    {
        return HttpOriginValidator.IsHttps(origin);
    }
}
