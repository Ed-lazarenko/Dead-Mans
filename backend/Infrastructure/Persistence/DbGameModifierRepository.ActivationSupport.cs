using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameModifierRepository
{
    private async Task<long> GetEarnedQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.UserId == userId
                && (x.EntryType == GameQuizPointEntryTypeValue.QuizReward
                    || x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment))
            .SumAsync(x => (long)x.PointsDelta, cancellationToken);
    }

    private async Task<long> GetSpentQuizPointsAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return -await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.GameId == gameId
                && x.UserId == userId
                && (x.EntryType == GameQuizPointEntryTypeValue.ModifierPurchase
                    || x.EntryType == GameQuizPointEntryTypeValue.ModifierRefund))
            .SumAsync(x => (long)x.PointsDelta, cancellationToken);
    }

    private static string? ResolveBlockedReason(
        bool emergencyDisabled,
        bool orderingClosed,
        bool isActiveTeamMember,
        bool limitReached,
        bool hasConflict,
        int availablePoints,
        int activationCost
    )
    {
        if (emergencyDisabled)
        {
            return "emergency_disabled";
        }

        if (orderingClosed)
        {
            return "ordering_closed";
        }

        if (isActiveTeamMember)
        {
            return "active_team_member";
        }

        if (limitReached)
        {
            return "limit_reached";
        }

        if (hasConflict)
        {
            return "conflict_active";
        }

        if (availablePoints < activationCost)
        {
            return "insufficient_points";
        }

        return null;
    }

    private async Task<bool> IsRoundTeamMemberAsync(
        Guid gameId,
        Guid roundId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        if (await _dbContext.GameRoundParticipants
                .AsNoTracking()
                .AnyAsync(
                    participant => participant.RoundId == roundId && participant.UserId == userId,
                    cancellationToken
                ))
        {
            return true;
        }

        return await _dbContext.GameTeamMembers
            .AsNoTracking()
            .AnyAsync(
                member =>
                    member.GameId == gameId
                    && member.TeamId == teamId
                    && member.UserId == userId
                    && member.LeftAtUtc == null,
                cancellationToken
            );
    }
}
