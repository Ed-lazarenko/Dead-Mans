using backend.Data;
using backend.Messaging;
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
            _logger.LogError(ex, AppMessages.Logs.ApplicationDbContextNotRegistered);
            throw new InvalidOperationException(AppMessages.Exceptions.AuthRequiresApplicationDbContext, ex);
        }

        var providerName = dbContext.Database.ProviderName;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            _logger.LogError(AppMessages.Logs.EfProviderNameEmpty);
            throw new InvalidOperationException(AppMessages.Exceptions.AuthRequiresEfProvider);
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
                _logger.LogError(ex, AppMessages.Logs.FailedToOpenDatabaseOnStartup);
                throw;
            }
        }

        _logger.LogInformation(AppMessages.Logs.AuthPersistenceValidated, providerName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
