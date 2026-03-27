using Microsoft.Extensions.Options;
using MsCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace backend.Infrastructure.Configuration;

/// <summary>
/// Registers the named CORS policy from <see cref="CorsOptions"/> so policy setup uses
/// <see cref="IOptions{CorsOptions}"/> instead of re-reading configuration.
/// </summary>
internal sealed class ConfigureDeadMansCorsPolicy : IConfigureOptions<MsCorsOptions>
{
    private readonly IOptions<CorsOptions> _cors;

    public ConfigureDeadMansCorsPolicy(IOptions<CorsOptions> cors)
    {
        _cors = cors;
    }

    public void Configure(MsCorsOptions options)
    {
        var cors = _cors.Value;
        options.AddPolicy(
            CorsPolicyNames.Default,
            policy =>
            {
                policy
                    .WithOrigins(cors.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        );
    }
}
