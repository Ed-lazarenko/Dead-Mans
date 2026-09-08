using System.ComponentModel.DataAnnotations;

namespace backend.Application.Abstractions.Auth;

public class TwitchAuthOptions
{
    public const string SectionName = "TwitchAuth";

    [Required]
    [MinLength(3)]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string ClientSecret { get; set; } = string.Empty;

    [Required]
    [Url]
    public string RedirectUri { get; set; } = string.Empty;

    [Required]
    [Url]
    public string FrontendRedirectUri { get; set; } = string.Empty;

    [Required]
    public string[] Scopes { get; set; } = [];

    public static bool IsValidRedirectUri(string? value, bool requireHttps)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return !requireHttps || uri.Scheme == Uri.UriSchemeHttps;
    }

    public static bool HasValidScopes(IEnumerable<string>? scopes)
    {
        if (scopes is null)
        {
            return false;
        }

        var normalized = scopes
            .Select(scope => scope?.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .ToArray();
        return normalized.Length > 0
            && normalized.Length == scopes.Count()
            && normalized.Distinct(StringComparer.Ordinal).Count() == normalized.Length;
    }
}
