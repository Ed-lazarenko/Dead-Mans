using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.GameModifiers;
using backend.Infrastructure.Persistence;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameModifierRepositoryTimingTests
{
    [Fact]
    public async Task ArchiveModifierAsync_UsesInjectedClock()
    {
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var modifierId = Guid.NewGuid();
        await TestModifierVersionFactory.AddAsync(
            dbContext,
            new TestModifierSpec(
                modifierId,
                "Clock modifier",
                "Verifies deterministic archive timestamps",
                GameModifierCategories.Round,
                1,
                null,
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Zhazhda).Behavior
            ),
            timestamp.AddDays(-1).UtcDateTime
        );
        var repository = new DbGameModifierRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var status = await repository.ArchiveModifierAsync(
            modifierId,
            expectedRevision: 1,
            new ModifierChangeActor(Guid.NewGuid(), "Clock Admin")
        );

        Assert.Equal(ArchiveGameModifierRepositoryStatus.Archived, status);
        var definition = await dbContext.ModifierDefinitions.SingleAsync();
        Assert.True(definition.IsArchived);
        Assert.Equal(timestamp.UtcDateTime, definition.ArchivedAtUtc);
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
