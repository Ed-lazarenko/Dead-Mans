namespace backend.Api.Configuration;

internal sealed class ProductionHostConfigurationStartupValidator : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ProductionHostConfigurationStartupValidator(
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        _configuration = configuration;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AllowedHostsValidator.Validate(_configuration, _environment);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
