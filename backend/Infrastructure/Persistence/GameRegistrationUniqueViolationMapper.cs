using backend.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal static class GameRegistrationUniqueViolationMapper
{
    internal static GameRegistrationErrorCode Map(
        DbUpdateException exception,
        GameRegistrationErrorCode slotTakenError = GameRegistrationErrorCode.NoAvailableSlot
    )
    {
        if (!PostgresUniqueViolation.TryGetConstraintName(exception, out var constraintName))
        {
            return GameRegistrationErrorCode.OperationFailed;
        }

        return constraintName switch
        {
            PostgresUniqueViolation.GameTeamsActiveSlot => slotTakenError,
            PostgresUniqueViolation.GameTeamInvitationsOnePendingPerUser =>
                GameRegistrationErrorCode.PendingInvitationExists,
            PostgresUniqueViolation.GameTeamMembersActiveGameUser
                or PostgresUniqueViolation.GameTeamMembersActiveTeamUser =>
                GameRegistrationErrorCode.UserAlreadyOnTeam,
            _ => GameRegistrationErrorCode.OperationFailed
        };
    }
}
