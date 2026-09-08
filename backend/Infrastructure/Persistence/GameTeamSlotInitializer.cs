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
        DateTime createdAtUtc,
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

        var teamSlots = GameRegistrationDefaults
            .BuildDefaultTeamSlots()
            .Select(
                slot =>
                    new GameTeamSlot
                    {
                        Id = Guid.NewGuid(),
                        GameId = gameId,
                        SlotIndex = slot.TeamSlotIndex,
                        SlotType = slot.TeamSlotType,
                        ReservedLabel = null,
                        CreatedAtUtc = createdAtUtc
                    }
            )
            .ToList();

        dbContext.GameTeamSlots.AddRange(teamSlots);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
