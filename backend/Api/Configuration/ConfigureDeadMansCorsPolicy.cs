using Microsoft.Extensions.Options;
using MsCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace backend.Api.Configuration;
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
        var allowedOrigins = cors.GetNormalizedAllowedOrigins();
        options.AddPolicy(
            CorsPolicyNames.Default,
            policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        );
    }
}
