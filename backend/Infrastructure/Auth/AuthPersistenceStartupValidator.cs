using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Auth;

public sealed class AuthPersistenceStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthPersistenceStartupValidator> _logger;

    public AuthPersistenceStartupValidator(
        IServiceProvider serviceProvider,
        ILogger<AuthPersistenceStartupValidator> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        ApplicationDbContext dbContext;
        try
        {
            dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "ApplicationDbContext is not registered. Auth requires a configured database."
            );
            throw new InvalidOperationException(
                "Authentication requires a configured ApplicationDbContext. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.",
                ex
            );
        }

        var providerName = dbContext.Database.ProviderName;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            _logger.LogError("EF Core provider name is empty; database is not configured.");
            throw new InvalidOperationException(
                "Authentication requires a configured EF Core provider. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests."
            );
        }

        if (dbContext.Database.IsRelational())
        {
            try
            {
                await dbContext.Database.OpenConnectionAsync(cancellationToken);
                await dbContext.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open database connection during startup validation.");
                throw;
            }
        }

        _logger.LogInformation(
            "Auth persistence validated: database provider is {ProviderName}.",
            providerName
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
