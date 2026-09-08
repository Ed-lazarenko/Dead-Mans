using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRegistrationPersistence : IGameRegistrationPersistence
{
    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistAssignPlayerAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_teams WHERE id = {teamId} FOR UPDATE""",
                    cancellationToken
                );
                var sourceTeamId = await (
                    from member in _dbContext.GameTeamMembers
                    join team in _dbContext.GameTeams on member.TeamId equals team.Id
                    where member.GameId == gameId
                        && member.UserId == userId
                        && member.LeftAtUtc == null
                        && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
                    select (Guid?)team.Id
                ).FirstOrDefaultAsync(cancellationToken);
                if (sourceTeamId.HasValue)
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""SELECT 1 FROM game_teams WHERE id = {sourceTeamId.Value} FOR UPDATE""",
                        cancellationToken
                    );
                }

                var result = await AssignPlayerCoreAsync(
                    gameId,
                    adminUserId,
                    teamId,
                    userId,
                    maxPlayersPerTeam,
                    cancellationToken
                );
                if (!result.Success)
                {
                    return result;
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }

            return await AssignPlayerCoreAsync(
                gameId,
                adminUserId,
                teamId,
                userId,
                maxPlayersPerTeam,
                cancellationToken
            );
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(ex, "Assign player failed due to unique constraint for game {GameId}.", gameId);
            return Fail<RegistrationTeamDto>(GameRegistrationUniqueViolationMapper.Map(ex));
        }
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistMoveTeamToSlotAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid targetTeamSlotId,
        CancellationToken cancellationToken = default
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_teams WHERE id = {teamId} FOR UPDATE""",
                cancellationToken
            );
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_team_slots WHERE game_id = {gameId} FOR UPDATE""",
                cancellationToken
            );

            var targetOccupyingTeamId = await _dbContext.GameTeams
                .Where(
                    candidate => candidate.GameId == gameId
                        && candidate.SlotId == targetTeamSlotId
                        && (candidate.Status == TeamStatusValue.Forming || candidate.Status == TeamStatusValue.Confirmed)
                )
                .Select(candidate => (Guid?)candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (targetOccupyingTeamId.HasValue)
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_teams WHERE id = {targetOccupyingTeamId.Value} FOR UPDATE""",
                    cancellationToken
                );
            }

            var result = await MoveTeamToSlotCoreAsync(
                gameId,
                adminUserId,
                teamId,
                targetTeamSlotId,
                cancellationToken
            );
            if (!result.Success)
            {
                return result;
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        return await MoveTeamToSlotCoreAsync(
            gameId,
            adminUserId,
            teamId,
            targetTeamSlotId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> PersistRemovePlayerFromTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var membership = await _dbContext.GameTeamMembers
            .Include(member => member.Team)
            .FirstOrDefaultAsync(
                member =>
                    member.GameId == gameId
                    && member.TeamId == teamId
                    && member.UserId == userId
                    && member.LeftAtUtc == null,
                cancellationToken
            );
        if (membership?.Team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = membership.Team;
        if (team.Status != TeamStatusValue.Forming && team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var utcNow = DateTime.UtcNow;
        membership.LeftAtUtc = utcNow;

        var remainingMembers = await _dbContext.GameTeamMembers.CountAsync(
            member =>
                member.TeamId == team.Id
                && member.LeftAtUtc == null
                && member.Id != membership.Id,
            cancellationToken
        );

        if (remainingMembers == 0)
        {
            team.Status = TeamStatusValue.Disbanded;
            team.DisbandedAtUtc = utcNow;
            team.DisbandedByUserId = adminUserId;
            team.ConfirmedAtUtc = null;
            team.ConfirmedByUserId = null;
            team.DisbandRequestedAtUtc = null;
            team.DisbandRequestedByUserId = null;

            var pendingInvitations = await _dbContext.GameTeamInvitations
                .Where(
                    invitation =>
                        invitation.TeamId == team.Id
                        && invitation.Status == TeamInvitationStatusValue.Pending
                )
                .ToListAsync(cancellationToken);
            foreach (var invitation in pendingInvitations)
            {
                invitation.Status = TeamInvitationStatusValue.Cancelled;
                invitation.RespondedAtUtc = utcNow;
            }
        }
        else if (team.Status == TeamStatusValue.Confirmed)
        {
            team.Status = TeamStatusValue.Forming;
            team.ConfirmedAtUtc = null;
            team.ConfirmedByUserId = null;
            team.DisbandRequestedAtUtc = null;
            team.DisbandRequestedByUserId = null;
        }

        team.UpdatedAtUtc = utcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} removed player {UserId} from team {TeamId} in game {GameId}.",
            adminUserId,
            userId,
            teamId,
            gameId
        );

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    public async Task<GameRegistrationResult<bool>> PersistCancelTeamInvitationAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var invitation = await _dbContext.GameTeamInvitations.FirstOrDefaultAsync(
            candidate =>
                candidate.Id == invitationId
                && candidate.GameId == gameId
                && candidate.TeamId == teamId,
            cancellationToken
        );
        if (invitation is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.InvitationNotFound);
        }

        if (invitation.Status != TeamInvitationStatusValue.Pending)
        {
            return Fail<bool>(GameRegistrationErrorCode.InvitationNotPending);
        }

        var utcNow = DateTime.UtcNow;
        invitation.Status = TeamInvitationStatusValue.Cancelled;
        invitation.RespondedAtUtc = utcNow;

        var team = await _dbContext.GameTeams.FirstOrDefaultAsync(
            candidate => candidate.Id == teamId && candidate.GameId == gameId,
            cancellationToken
        );
        if (team is not null)
        {
            team.UpdatedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} cancelled invitation {InvitationId} for team {TeamId} in game {GameId}.",
            adminUserId,
            invitationId,
            teamId,
            gameId
        );

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistConfirmTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        short minPlayersPerTeam,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_teams WHERE id = {teamId} FOR UPDATE""",
                cancellationToken
            );

            var result = await ConfirmTeamCoreAsync(
                gameId,
                adminUserId,
                teamId,
                minPlayersPerTeam,
                maxPlayersPerTeam,
                cancellationToken
            );
            if (!result.Success)
            {
                return result;
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        return await ConfirmTeamCoreAsync(
            gameId,
            adminUserId,
            teamId,
            minPlayersPerTeam,
            maxPlayersPerTeam,
            cancellationToken
        );
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> ConfirmTeamCoreAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        short minPlayersPerTeam,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken
    )
    {
        var team = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == team.Id && member.LeftAtUtc == null,
            cancellationToken
        );
        if (memberCount < minPlayersPerTeam || memberCount > maxPlayersPerTeam)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var hasPendingInvitation = await _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.TeamId == team.Id
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
        if (hasPendingInvitation)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.PendingOutgoingInvitation);
        }

        var utcNow = DateTime.UtcNow;
        team.Status = TeamStatusValue.Confirmed;
        team.ConfirmedAtUtc = utcNow;
        team.ConfirmedByUserId = adminUserId;
        team.UpdatedAtUtc = utcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

    public async Task<GameRegistrationResult<bool>> PersistRejectTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var team = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var utcNow = DateTime.UtcNow;
        var members = await _dbContext.GameTeamMembers
            .Where(member => member.TeamId == team.Id && member.LeftAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.LeftAtUtc = utcNow;
        }

        team.Status = TeamStatusValue.Rejected;
        team.RejectedAtUtc = utcNow;
        team.RejectedByUserId = adminUserId;
        team.UpdatedAtUtc = utcNow;

        var pendingInvitations = await _dbContext.GameTeamInvitations
            .Where(
                invitation => invitation.TeamId == team.Id
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .ToListAsync(cancellationToken);
        foreach (var invitation in pendingInvitations)
        {
            invitation.Status = TeamInvitationStatusValue.Cancelled;
            invitation.RespondedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Team {TeamId} rejected by admin {AdminUserId}.",
            teamId,
            adminUserId
        );

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    public async Task<GameRegistrationResult<bool>> PersistDisbandConfirmedTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var team = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var utcNow = DateTime.UtcNow;
        var members = await _dbContext.GameTeamMembers
            .Where(member => member.TeamId == team.Id && member.LeftAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var member in members)
        {
            member.LeftAtUtc = utcNow;
        }

        team.Status = TeamStatusValue.Disbanded;
        team.DisbandedAtUtc = utcNow;
        team.DisbandedByUserId = adminUserId;
        team.UpdatedAtUtc = utcNow;

        var pendingInvitations = await _dbContext.GameTeamInvitations
            .Where(
                invitation => invitation.TeamId == team.Id
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .ToListAsync(cancellationToken);
        foreach (var invitation in pendingInvitations)
        {
            invitation.Status = TeamInvitationStatusValue.Cancelled;
            invitation.RespondedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Confirmed team {TeamId} disbanded by admin {AdminUserId}.",
            teamId,
            adminUserId
        );

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

}
