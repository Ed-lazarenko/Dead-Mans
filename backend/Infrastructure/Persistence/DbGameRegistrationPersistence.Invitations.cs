using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRegistrationPersistence : IGameRegistrationPersistence
{
    public async Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreateAdminInvitationAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamSlotId,
        int teamSlotIndex,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        return await PersistCreateInvitationAsync(
            gameId,
            adminUserId,
            teamSlotId,
            teamSlotIndex,
            invitedUserId,
            teamId,
            InvitedByKindValue.Admin,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreatePlayerInvitationAsync(
        Guid gameId,
        Guid userId,
        Guid teamSlotId,
        int teamSlotIndex,
        Guid invitedUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        return await PersistCreateInvitationAsync(
            gameId,
            userId,
            teamSlotId,
            teamSlotIndex,
            invitedUserId,
            teamId,
            InvitedByKindValue.Member,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> PersistCancelPlayerInvitationAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var invitation = await _dbContext.GameTeamInvitations.FirstOrDefaultAsync(
            candidate =>
                candidate.Id == invitationId
                && candidate.GameId == gameId
                && candidate.TeamId == teamId
                && candidate.InvitedByUserId == userId,
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

        invitation.Status = TeamInvitationStatusValue.Cancelled;
        invitation.RespondedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    private async Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreateInvitationAsync(
        Guid gameId,
        Guid invitedByUserId,
        Guid slotId,
        int slotIndex,
        Guid invitedUserId,
        Guid? teamId,
        string invitedByKind,
        CancellationToken cancellationToken
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM game_team_slots WHERE id = {slotId} FOR UPDATE""",
                cancellationToken
            );
            if (teamId.HasValue)
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_teams WHERE id = {teamId.Value} FOR UPDATE""",
                    cancellationToken
                );
            }

            var validationError = await ValidateInvitationTargetAsync(
                gameId,
                slotId,
                invitedUserId,
                teamId,
                cancellationToken
            );
            if (validationError != GameRegistrationErrorCode.None)
            {
                return Fail<RegistrationInvitationDto>(validationError);
            }

            var transactionalResult = await SaveInvitationAsync(
                gameId,
                invitedByUserId,
                slotId,
                slotIndex,
                invitedUserId,
                teamId,
                invitedByKind,
                cancellationToken
            );
            if (!transactionalResult.Success)
            {
                return transactionalResult;
            }

            await transaction.CommitAsync(cancellationToken);
            return transactionalResult;
        }

        var inMemoryValidationError = await ValidateInvitationTargetAsync(
            gameId,
            slotId,
            invitedUserId,
            teamId,
            cancellationToken
        );
        if (inMemoryValidationError != GameRegistrationErrorCode.None)
        {
            return Fail<RegistrationInvitationDto>(inMemoryValidationError);
        }

        return await SaveInvitationAsync(
            gameId,
            invitedByUserId,
            slotId,
            slotIndex,
            invitedUserId,
            teamId,
            invitedByKind,
            cancellationToken
        );
    }

    private async Task<GameRegistrationResult<RegistrationInvitationDto>> SaveInvitationAsync(
        Guid gameId,
        Guid invitedByUserId,
        Guid slotId,
        int slotIndex,
        Guid invitedUserId,
        Guid? teamId,
        string invitedByKind,
        CancellationToken cancellationToken
    )
    {
        var invitation = new GameTeamInvitation
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SlotId = slotId,
            TeamId = teamId,
            InvitedUserId = invitedUserId,
            InvitedByUserId = invitedByUserId,
            InvitedByKind = invitedByKind,
            Status = TeamInvitationStatusValue.Pending,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.GameTeamInvitations.Add(invitation);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(
                ex,
                "Create invitation failed due to unique constraint for game {GameId}.",
                gameId
            );
            return Fail<RegistrationInvitationDto>(GameRegistrationUniqueViolationMapper.Map(ex));
        }

        var dto = new RegistrationInvitationDto(
            invitation.Id,
            slotId,
            slotIndex,
            teamId,
            invitation.Status,
            invitation.CreatedAtUtc,
            null,
            null
        );
        return new GameRegistrationResult<RegistrationInvitationDto>(true, dto, GameRegistrationErrorCode.None);
    }

    private async Task<GameRegistrationErrorCode> ValidateInvitationTargetAsync(
        Guid gameId,
        Guid slotId,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken
    )
    {
        var invitedUserExistsAndActive = await _dbContext.Users.AnyAsync(
            user => user.Id == invitedUserId && user.IsActive,
            cancellationToken
        );
        if (!invitedUserExistsAndActive)
        {
            return GameRegistrationErrorCode.UserNotFound;
        }

        var slotExists = await _dbContext.GameTeamSlots.AnyAsync(
            slot => slot.Id == slotId && slot.GameId == gameId,
            cancellationToken
        );
        if (!slotExists)
        {
            return GameRegistrationErrorCode.SlotNotFound;
        }

        var userAlreadyOnTeam = await (
            from member in _dbContext.GameTeamMembers
            join team in _dbContext.GameTeams on member.TeamId equals team.Id
            where member.GameId == gameId
                && member.UserId == invitedUserId
                && member.LeftAtUtc == null
                && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            select member.Id
        ).AnyAsync(cancellationToken);
        if (userAlreadyOnTeam)
        {
            return GameRegistrationErrorCode.UserAlreadyOnTeam;
        }

        var hasPendingInvitationForUser = await _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.GameId == gameId
                && invitation.InvitedUserId == invitedUserId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
        if (hasPendingInvitationForUser)
        {
            return GameRegistrationErrorCode.PendingInvitationExists;
        }

        if (!teamId.HasValue)
        {
            return GameRegistrationErrorCode.TeamInviteNotAllowed;
        }

        if (teamId.HasValue)
        {
            var team = await _dbContext.GameTeams
                .Where(candidate => candidate.Id == teamId.Value && candidate.GameId == gameId)
                .Select(candidate => new { candidate.SlotId, candidate.Status, candidate.RecruitmentOpen })
                .FirstOrDefaultAsync(cancellationToken);
            if (team is null)
            {
                return GameRegistrationErrorCode.TeamNotFound;
            }

            if (team.SlotId != slotId || team.Status != TeamStatusValue.Forming)
            {
                return GameRegistrationErrorCode.TeamNotJoinable;
            }

            if (team.RecruitmentOpen)
            {
                return GameRegistrationErrorCode.TeamInviteNotAllowed;
            }

            var maxPlayersPerTeam = await _dbContext.Games
                .Where(game => game.Id == gameId)
                .Select(game => game.MaxPlayersPerTeam)
                .FirstAsync(cancellationToken);
            var activeMemberCount = await _dbContext.GameTeamMembers.CountAsync(
                member => member.TeamId == teamId.Value && member.LeftAtUtc == null,
                cancellationToken
            );
            var pendingInvitationCount = await _dbContext.GameTeamInvitations.CountAsync(
                invitation =>
                    invitation.TeamId == teamId.Value
                    && invitation.Status == TeamInvitationStatusValue.Pending,
                cancellationToken
            );
            if (activeMemberCount + pendingInvitationCount >= maxPlayersPerTeam)
            {
                return GameRegistrationErrorCode.TeamFull;
            }

            return GameRegistrationErrorCode.None;
        }

        var slotAlreadyBlockedByPendingInvite = await _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.GameId == gameId
                && invitation.SlotId == slotId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
        if (slotAlreadyBlockedByPendingInvite)
        {
            return GameRegistrationErrorCode.SlotNotAvailable;
        }

        var slotAlreadyOccupiedByTeam = await _dbContext.GameTeams.AnyAsync(
            team =>
                team.GameId == gameId
                && team.SlotId == slotId
                && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed),
            cancellationToken
        );
        if (slotAlreadyOccupiedByTeam)
        {
            return GameRegistrationErrorCode.SlotNotAvailable;
        }

        return GameRegistrationErrorCode.None;
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistAcceptInvitationAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var acceptResult = await AcceptInvitationCoreAsync(command, cancellationToken);
            if (!acceptResult.Success)
            {
                return acceptResult;
            }

            await transaction.CommitAsync(cancellationToken);
            return acceptResult;
        }

        try
        {
            return await AcceptInvitationCoreAsync(command, cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(ex, "Accept invitation failed due to unique constraint.");
            return Fail<RegistrationTeamDto>(
                GameRegistrationUniqueViolationMapper.Map(ex, GameRegistrationErrorCode.SlotNotAvailable)
            );
        }
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> AcceptInvitationCoreAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (_dbContext.Database.IsRelational())
            {
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM games WHERE id = {command.GameId} FOR UPDATE""",
                    cancellationToken
                );
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_team_slots WHERE id = {command.TeamSlotId} AND game_id = {command.GameId} FOR UPDATE""",
                    cancellationToken
                );
                if (command.TeamId.HasValue)
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"""SELECT 1 FROM game_teams WHERE id = {command.TeamId.Value} AND game_id = {command.GameId} FOR UPDATE""",
                        cancellationToken
                    );
                }

                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_team_invitations WHERE id = {command.InvitationId} FOR UPDATE""",
                    cancellationToken
                );
            }

            var invitation = await _dbContext.GameTeamInvitations
                .FirstOrDefaultAsync(
                    candidate =>
                        candidate.Id == command.InvitationId
                        && candidate.GameId == command.GameId
                        && candidate.InvitedUserId == command.UserId
                        && candidate.SlotId == command.TeamSlotId
                        && candidate.TeamId == command.TeamId,
                    cancellationToken
                );
            if (invitation is null)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvitationNotFound);
            }

            var gameIsReady = await _dbContext.Games
                .AsNoTracking()
                .AnyAsync(
                    candidate =>
                        candidate.Id == command.GameId
                        && candidate.Status == GameStatusValue.Ready
                        && !candidate.IsDeleted,
                    cancellationToken
                );
            if (!gameIsReady)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
            }

            if (invitation.Status != TeamInvitationStatusValue.Pending)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvitationNotPending);
            }

            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            GameTeam team;
            if (command.TeamId.HasValue)
            {
                var existingTeam = await _dbContext.GameTeams
                    .FirstOrDefaultAsync(
                        candidate =>
                            candidate.Id == command.TeamId.Value
                            && candidate.GameId == command.GameId,
                        cancellationToken
                    );
                if (existingTeam is null)
                {
                    return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
                }

                team = existingTeam;
                if (team.Status != TeamStatusValue.Forming || team.SlotId != command.TeamSlotId)
                {
                    return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
                }

                var memberCount = await _dbContext.GameTeamMembers.CountAsync(
                    member => member.TeamId == team.Id && member.LeftAtUtc == null,
                    cancellationToken
                );
                if (memberCount >= command.MaxPlayersPerTeam)
                {
                    return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamFull);
                }

                _dbContext.GameTeamMembers.Add(
                    new GameTeamMember
                    {
                        Id = Guid.NewGuid(),
                        GameId = command.GameId,
                        TeamId = team.Id,
                        UserId = command.UserId,
                        JoinedAtUtc = utcNow
                    }
                );
                team.UpdatedAtUtc = utcNow;
            }
            else
            {
                team = new GameTeam
                {
                    Id = Guid.NewGuid(),
                    GameId = command.GameId,
                    SlotId = command.TeamSlotId,
                    RecruitmentOpen = false,
                    Status = TeamStatusValue.Forming,
                    CreatedByUserId = command.UserId,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };
                _dbContext.GameTeams.Add(team);
                _dbContext.GameTeamMembers.Add(
                    new GameTeamMember
                    {
                        Id = Guid.NewGuid(),
                        GameId = command.GameId,
                        TeamId = team.Id,
                        UserId = command.UserId,
                        JoinedAtUtc = utcNow
                    }
                );
            }

            invitation.Status = TeamInvitationStatusValue.Accepted;
            invitation.RespondedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await LoadTeamResultAsync(team.Id, cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(ex, "Accept invitation failed due to unique constraint.");
            return Fail<RegistrationTeamDto>(
                GameRegistrationUniqueViolationMapper.Map(ex, GameRegistrationErrorCode.SlotNotAvailable)
            );
        }
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> AddJoiningMemberAsync(
        Guid gameId,
        Guid userId,
        GameTeam team,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken
    )
    {
        if (team.Status != TeamStatusValue.Forming || !team.RecruitmentOpen)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == team.Id && member.LeftAtUtc == null,
            cancellationToken
        );
        if (memberCount >= maxPlayersPerTeam)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamFull);
        }

        _dbContext.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = team.Id,
                UserId = userId,
                JoinedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            }
        );
        team.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

    public async Task<GameRegistrationResult<bool>> PersistDeclineInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var invitation = await _dbContext.GameTeamInvitations
            .FirstOrDefaultAsync(candidate => candidate.Id == invitationId, cancellationToken);
        if (invitation is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.InvitationNotFound);
        }

        if (invitation.InvitedUserId != userId
            || invitation.Status != TeamInvitationStatusValue.Pending)
        {
            return Fail<bool>(GameRegistrationErrorCode.InvitationNotPending);
        }

        invitation.Status = TeamInvitationStatusValue.Declined;
        invitation.RespondedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

}
