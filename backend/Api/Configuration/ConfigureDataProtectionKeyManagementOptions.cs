using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace backend.Api.Configuration;

internal sealed class ConfigureDataProtectionKeyManagementOptions
    : IConfigureOptions<KeyManagementOptions>
{
    private readonly IOptions<DataProtectionSecurityOptions> _securityOptions;
    private readonly ILoggerFactory _loggerFactory;

    public ConfigureDataProtectionKeyManagementOptions(
        IOptions<DataProtectionSecurityOptions> securityOptions,
        ILoggerFactory loggerFactory
    )
    {
        _securityOptions = securityOptions;
        _loggerFactory = loggerFactory;
    }

    public void Configure(KeyManagementOptions options)
    {
        var keysDirectory = _securityOptions.Value.KeysDirectory;
        if (string.IsNullOrWhiteSpace(keysDirectory))
        {
            return;
        }

        options.XmlRepository = new FileSystemXmlRepository(
            new DirectoryInfo(Path.GetFullPath(keysDirectory)),
            _loggerFactory
        );
    }
}
