using backend.Application.Configuration;
using backend.Data;
using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public static class GameTeamSlotInitializer
{
    public static async Task EnsureDefaultSlotsAsync(
        ApplicationDbContext dbContext,
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var hasSlots = await dbContext.GameTeamSlots.AnyAsync(
            slot => slot.GameId == gameId,
            cancellationToken
        );
        if (hasSlots)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        var slots = GameRegistrationDefaults
            .BuildDefaultSlots()
            .Select(
                slot =>
                    new GameTeamSlot
                    {
                        Id = Guid.NewGuid(),
                        GameId = gameId,
                        SlotIndex = slot.SlotIndex,
                        Availability = slot.Availability,
                        ReservedLabel = null,
                        CreatedAtUtc = utcNow
                    }
            )
            .ToList();

        dbContext.GameTeamSlots.AddRange(slots);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
