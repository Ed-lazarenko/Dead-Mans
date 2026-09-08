namespace backend.Infrastructure.Configuration;

internal sealed class DatabaseConfigurationStartupValidator : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public DatabaseConfigurationStartupValidator(
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        _configuration = configuration;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = DatabaseConnectionStringResolver.ResolveAndValidate(_configuration, _environment);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
