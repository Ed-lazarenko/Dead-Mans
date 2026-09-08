using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRegistrationPersistence : IGameRegistrationPersistence
{
    private async Task<GameRegistrationResult<RegistrationTeamDto>> AssignPlayerCoreAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken
    )
    {
        var targetTeam = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (targetTeam is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (targetTeam.Status != TeamStatusValue.Forming && targetTeam.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var targetMemberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == targetTeam.Id && member.LeftAtUtc == null,
            cancellationToken
        );

        var activeMembership = await _dbContext.GameTeamMembers
            .Include(member => member.Team)
            .FirstOrDefaultAsync(
                member => member.GameId == gameId && member.UserId == userId && member.LeftAtUtc == null,
                cancellationToken
            );

        if (activeMembership is not null && activeMembership.TeamId == targetTeam.Id)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TargetTeamSameAsSource);
        }

        if (activeMembership is null && targetMemberCount >= maxPlayersPerTeam)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamFull);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (activeMembership is not null)
        {
            if (targetMemberCount >= maxPlayersPerTeam)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamFull);
            }

            activeMembership.LeftAtUtc = utcNow;

            if (activeMembership.Team is not null)
            {
                var remainingSourceMembers = await _dbContext.GameTeamMembers.CountAsync(
                    member =>
                        member.TeamId == activeMembership.TeamId
                        && member.LeftAtUtc == null
                        && member.Id != activeMembership.Id,
                    cancellationToken
                );

                if (remainingSourceMembers == 0)
                {
                    activeMembership.Team.Status = TeamStatusValue.Disbanded;
                    activeMembership.Team.DisbandedAtUtc = utcNow;
                    activeMembership.Team.DisbandedByUserId = userId;
                    activeMembership.Team.ConfirmedAtUtc = null;
                    activeMembership.Team.ConfirmedByUserId = null;

                    var pendingInvitations = await _dbContext.GameTeamInvitations
                        .Where(
                            invitation =>
                                invitation.TeamId == activeMembership.TeamId
                                && invitation.Status == TeamInvitationStatusValue.Pending
                        )
                        .ToListAsync(cancellationToken);
                    foreach (var invitation in pendingInvitations)
                    {
                        invitation.Status = TeamInvitationStatusValue.Cancelled;
                        invitation.RespondedAtUtc = utcNow;
                    }
                }
                else if (activeMembership.Team.Status == TeamStatusValue.Confirmed)
                {
                    activeMembership.Team.Status = TeamStatusValue.Forming;
                    activeMembership.Team.ConfirmedAtUtc = null;
                    activeMembership.Team.ConfirmedByUserId = null;
                }

                activeMembership.Team.UpdatedAtUtc = utcNow;
            }
        }

        _dbContext.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = targetTeam.Id,
                UserId = userId,
                JoinedAtUtc = utcNow
            }
        );

        if (targetTeam.Status == TeamStatusValue.Confirmed)
        {
            targetTeam.Status = TeamStatusValue.Forming;
            targetTeam.ConfirmedAtUtc = null;
            targetTeam.ConfirmedByUserId = null;
        }

        targetTeam.UpdatedAtUtc = utcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminUserId} assigned player {UserId} to team {TeamId} in game {GameId}.",
            adminUserId,
            userId,
            teamId,
            gameId
        );

        return await LoadTeamResultAsync(targetTeam.Id, cancellationToken);
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> MoveTeamToSlotCoreAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid targetSlotId,
        CancellationToken cancellationToken
    )
    {
        var sourceTeam = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (sourceTeam is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        var targetSlotExists = await _dbContext.GameTeamSlots.AnyAsync(
            slot => slot.Id == targetSlotId && slot.GameId == gameId,
            cancellationToken
        );
        if (!targetSlotExists)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotFound);
        }

        if (sourceTeam.SlotId == targetSlotId)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotAvailable);
        }

        var targetTeam = await _dbContext.GameTeams.FirstOrDefaultAsync(
            candidate => candidate.GameId == gameId
                && candidate.SlotId == targetSlotId
                && (candidate.Status == TeamStatusValue.Forming || candidate.Status == TeamStatusValue.Confirmed),
            cancellationToken
        );

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var originalSlotId = sourceTeam.SlotId;

        if (targetTeam is null)
        {
            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            await MovePendingTeamInvitationsAsync(sourceTeam.Id, targetSlotId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (_dbContext.Database.IsRelational())
        {
            var swapBufferSlot = new GameTeamSlot
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SlotIndex = await GetNextSwapBufferSlotIndexAsync(gameId, cancellationToken),
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = utcNow
            };
            _dbContext.GameTeamSlots.Add(swapBufferSlot);
            await _dbContext.SaveChangesAsync(cancellationToken);

            sourceTeam.SlotId = swapBufferSlot.Id;
            sourceTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            targetTeam.SlotId = originalSlotId;
            targetTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await MovePendingTeamInvitationsAsync(sourceTeam.Id, targetSlotId, cancellationToken);
            await MovePendingTeamInvitationsAsync(targetTeam.Id, originalSlotId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.GameTeamSlots.Remove(swapBufferSlot);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            targetTeam.SlotId = originalSlotId;
            targetTeam.UpdatedAtUtc = utcNow;
            await MovePendingTeamInvitationsAsync(sourceTeam.Id, targetSlotId, cancellationToken);
            await MovePendingTeamInvitationsAsync(targetTeam.Id, originalSlotId, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Admin {AdminUserId} moved team {TeamId} to slot {TargetSlotId} in game {GameId}.",
            adminUserId,
            teamId,
            targetSlotId,
            gameId
        );

        return await LoadTeamResultAsync(sourceTeam.Id, cancellationToken);
    }

    private async Task MovePendingTeamInvitationsAsync(
        Guid teamId,
        Guid targetSlotId,
        CancellationToken cancellationToken
    )
    {
        var pendingInvitations = await _dbContext.GameTeamInvitations
            .Where(invitation =>
                invitation.TeamId == teamId
                && invitation.Status == TeamInvitationStatusValue.Pending)
            .ToArrayAsync(cancellationToken);
        foreach (var invitation in pendingInvitations)
        {
            invitation.SlotId = targetSlotId;
        }
    }

    private async Task<int> GetNextSwapBufferSlotIndexAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var maxSlotIndex = await _dbContext.GameTeamSlots
            .Where(slot => slot.GameId == gameId)
            .MaxAsync(slot => (int?)slot.SlotIndex, cancellationToken);
        return (maxSlotIndex ?? 0) + 1;
    }

}
