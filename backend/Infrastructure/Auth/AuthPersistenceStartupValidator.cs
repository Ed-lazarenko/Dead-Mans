using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Auth;

public sealed class AuthPersistenceStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public AuthPersistenceStartupValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
            throw new InvalidOperationException(
                "Authentication requires a configured ApplicationDbContext. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.",
                ex
            );
        }

        var providerName = dbContext.Database.ProviderName;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException(
                "Authentication requires a configured EF Core provider. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests."
            );
        }

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
