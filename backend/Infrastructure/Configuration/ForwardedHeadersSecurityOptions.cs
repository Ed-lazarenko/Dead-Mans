using System.ComponentModel.DataAnnotations;

namespace backend.Infrastructure.Configuration;

public sealed class ForwardedHeadersSecurityOptions
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; set; } = true;

    [Required]
    public string[] TrustedProxies { get; set; } = [];

    [Required]
    public string[] TrustedNetworks { get; set; } = [];

    public bool TrustAllProxiesInDevelopment { get; set; } = true;
}
