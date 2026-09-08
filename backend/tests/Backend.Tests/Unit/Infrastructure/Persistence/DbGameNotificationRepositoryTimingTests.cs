using backend.Data;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameNotificationRepositoryTimingTests
{
    [Fact]
    public async Task CreateAndReadNotifications_UseInjectedClock()
    {
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var repository = new DbGameNotificationRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var created = await repository.CreateModifierCancelledNotificationAsync(
            userId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Clock modifier",
            "Clock Admin",
            5
        );
        await repository.MarkAllReadAsync(userId);

        Assert.Equal(timestamp.UtcDateTime, created.CreatedAtUtc);
        var entity = await dbContext.GameUserNotifications.SingleAsync();
        Assert.Equal(timestamp.UtcDateTime, entity.CreatedAtUtc);
        Assert.Equal(timestamp.UtcDateTime, entity.ReadAtUtc);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
