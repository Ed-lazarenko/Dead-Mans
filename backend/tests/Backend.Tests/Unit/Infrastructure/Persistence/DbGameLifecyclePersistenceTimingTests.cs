using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameLifecyclePersistenceTimingTests
{
    [Fact]
    public async Task OpenRegistrationAsync_UsesInjectedClockForPublicationAndDefaultSlots()
    {
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var gameId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Clock draft",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = timestamp.AddDays(-1).UtcDateTime,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 4
            }
        );
        await dbContext.SaveChangesAsync();
        var persistence = CreatePersistence(dbContext, timestamp);

        var result = await persistence.OpenRegistrationAsync(gameId);

        Assert.True(result.Success);
        var game = await dbContext.Games.SingleAsync();
        Assert.Equal(GameStatusValue.Ready, game.Status);
        Assert.Equal(timestamp.UtcDateTime, game.ReadyAtUtc);
        var slots = await dbContext.GameTeamSlots.ToArrayAsync();
        Assert.NotEmpty(slots);
        Assert.All(slots, slot => Assert.Equal(timestamp.UtcDateTime, slot.CreatedAtUtc));
    }

    [Fact]
    public async Task ArchiveGameAsync_UsesInjectedClock()
    {
        var timestamp = new DateTimeOffset(2036, 5, 6, 7, 8, 9, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var gameId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Clock finished game",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = timestamp.AddDays(-2).UtcDateTime,
                ReadyAtUtc = timestamp.AddDays(-2).UtcDateTime,
                StartedAtUtc = timestamp.AddDays(-1).UtcDateTime,
                FinishedAtUtc = timestamp.AddHours(-1).UtcDateTime,
                MinPlayersPerTeam = 1,
                MaxPlayersPerTeam = 4
            }
        );
        await dbContext.SaveChangesAsync();
        var persistence = CreatePersistence(dbContext, timestamp);

        var result = await persistence.ArchiveGameAsync(gameId);

        Assert.True(result.Success);
        var game = await dbContext.Games.SingleAsync();
        Assert.True(game.IsDeleted);
        Assert.Equal(timestamp.UtcDateTime, game.DeletedAtUtc);
    }

    private static DbGameLifecyclePersistence CreatePersistence(
        ApplicationDbContext dbContext,
        DateTimeOffset utcNow
    ) =>
        new(
            dbContext,
            NullLogger<DbGameLifecyclePersistence>.Instance,
            new FixedTimeProvider(utcNow)
        );

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
