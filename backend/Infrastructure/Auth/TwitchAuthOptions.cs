using System.ComponentModel.DataAnnotations;

namespace backend.Infrastructure.Auth;

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
    public string[] Scopes { get; set; } = ["openid", "user:read:email"];
}
