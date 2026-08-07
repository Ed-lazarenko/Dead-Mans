using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameRegistrationPersistence : IGameRegistrationPersistence
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGameRegistrationReadStore _reads;
    private readonly ILogger<DbGameRegistrationPersistence> _logger;

    public DbGameRegistrationPersistence(
        ApplicationDbContext dbContext,
        IGameRegistrationReadStore reads,
        ILogger<DbGameRegistrationPersistence> logger
    )
    {
        _dbContext = dbContext;
        _reads = reads;
        _logger = logger;
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistCreateTeamAsync(
        Guid gameId,
        Guid userId,
        Guid slotId,
        bool recruitmentOpen,
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = DateTime.UtcNow;
        var team = new GameTeam
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SlotId = slotId,
            RecruitmentOpen = recruitmentOpen,
            Status = TeamStatusValue.Forming,
            CreatedByUserId = userId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        var member = new GameTeamMember
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            TeamId = team.Id,
            UserId = userId,
            JoinedAtUtc = utcNow
        };

        try
        {
            _dbContext.GameTeams.Add(team);
            _dbContext.GameTeamMembers.Add(member);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(ex, "Create team failed due to unique constraint for game {GameId}.", gameId);
            return Fail<RegistrationTeamDto>(GameRegistrationUniqueViolationMapper.Map(ex));
        }

        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistCreateEmptyTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid slotId,
        bool recruitmentOpen,
        CancellationToken cancellationToken = default
    )
    {
        var utcNow = DateTime.UtcNow;
        var team = new GameTeam
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SlotId = slotId,
            RecruitmentOpen = recruitmentOpen,
            Status = TeamStatusValue.Forming,
            CreatedByUserId = adminUserId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        try
        {
            _dbContext.GameTeams.Add(team);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(
                ex,
                "Create empty team failed due to unique constraint for game {GameId}.",
                gameId
            );
            return Fail<RegistrationTeamDto>(GameRegistrationUniqueViolationMapper.Map(ex));
        }

        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistJoinTeamAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    )
    {
        var team = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        try
        {
            if (_dbContext.Database.IsRelational())
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT 1 FROM game_teams WHERE id = {team.Id} FOR UPDATE""",
                    cancellationToken
                );
                await _dbContext.Entry(team).ReloadAsync(cancellationToken);

                var joinResult = await AddJoiningMemberAsync(gameId, userId, team, maxPlayersPerTeam, cancellationToken);
                if (!joinResult.Success)
                {
                    return joinResult;
                }

                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                var joinResult = await AddJoiningMemberAsync(gameId, userId, team, maxPlayersPerTeam, cancellationToken);
                if (!joinResult.Success)
                {
                    return joinResult;
                }
            }
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out _))
        {
            _logger.LogWarning(ex, "Join team failed due to unique constraint for game {GameId}.", gameId);
            return Fail<RegistrationTeamDto>(GameRegistrationUniqueViolationMapper.Map(ex));
        }

        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

    public async Task<GameRegistrationResult<bool>> PersistLeaveTeamAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var membership = await _dbContext.GameTeamMembers
            .Include(member => member.Team)
            .FirstOrDefaultAsync(
                member => member.GameId == gameId && member.UserId == userId && member.LeftAtUtc == null,
                cancellationToken
            );
        if (membership?.Team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.NotTeamMember);
        }

        var utcNow = DateTime.UtcNow;
        var team = membership.Team;
        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == team.Id && member.LeftAtUtc == null,
            cancellationToken
        );
        membership.LeftAtUtc = utcNow;

        if (memberCount <= 1)
        {
            team.Status = TeamStatusValue.Disbanded;
            team.DisbandedAtUtc = utcNow;
            team.DisbandedByUserId = userId;
            team.UpdatedAtUtc = utcNow;
        }
        else
        {
            if (team.Status == TeamStatusValue.Confirmed)
            {
                team.Status = TeamStatusValue.Forming;
                team.ConfirmedAtUtc = null;
                team.ConfirmedByUserId = null;
            }

            team.UpdatedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> PersistRequestTeamDisbandAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var team = await _dbContext.GameTeams
            .FirstOrDefaultAsync(candidate => candidate.Id == teamId && candidate.GameId == gameId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var isActiveMember = await _dbContext.GameTeamMembers.AnyAsync(
            member =>
                member.GameId == gameId
                && member.TeamId == teamId
                && member.UserId == userId
                && member.LeftAtUtc == null,
            cancellationToken
        );
        if (!isActiveMember)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NotTeamMember);
        }

        if (team.DisbandRequestedAtUtc is null)
        {
            var utcNow = DateTime.UtcNow;
            team.DisbandRequestedAtUtc = utcNow;
            team.DisbandRequestedByUserId = userId;
            team.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await LoadTeamResultAsync(team.Id, cancellationToken);
    }

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
        Guid targetSlotId,
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
                        && candidate.SlotId == targetSlotId
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
                targetSlotId,
                cancellationToken
            );
            if (!result.Success)
            {
                return result;
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        return await MoveTeamToSlotCoreAsync(gameId, adminUserId, teamId, targetSlotId, cancellationToken);
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

    public async Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreateAdminInvitationAsync(
        Guid gameId,
        Guid adminUserId,
        Guid slotId,
        int slotIndex,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        return await PersistCreateInvitationAsync(
            gameId,
            adminUserId,
            slotId,
            slotIndex,
            invitedUserId,
            teamId,
            InvitedByKindValue.Admin,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreatePlayerInvitationAsync(
        Guid gameId,
        Guid userId,
        Guid slotId,
        int slotIndex,
        Guid invitedUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        return await PersistCreateInvitationAsync(
            gameId,
            userId,
            slotId,
            slotIndex,
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
        invitation.RespondedAtUtc = DateTime.UtcNow;
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
            CreatedAtUtc = DateTime.UtcNow
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

        var utcNow = DateTime.UtcNow;
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

        var utcNow = DateTime.UtcNow;
        var originalSlotId = sourceTeam.SlotId;

        if (targetTeam is null)
        {
            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (_dbContext.Database.IsRelational())
        {
            var temporarySlot = new GameTeamSlot
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SlotIndex = await GetNextTemporarySlotIndexAsync(gameId, cancellationToken),
                Availability = SlotAvailabilityValue.Public,
                CreatedAtUtc = utcNow
            };
            _dbContext.GameTeamSlots.Add(temporarySlot);
            await _dbContext.SaveChangesAsync(cancellationToken);

            sourceTeam.SlotId = temporarySlot.Id;
            sourceTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            targetTeam.SlotId = originalSlotId;
            targetTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.GameTeamSlots.Remove(temporarySlot);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            sourceTeam.SlotId = targetSlotId;
            sourceTeam.UpdatedAtUtc = utcNow;
            targetTeam.SlotId = originalSlotId;
            targetTeam.UpdatedAtUtc = utcNow;
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

    private async Task<int> GetNextTemporarySlotIndexAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var maxSlotIndex = await _dbContext.GameTeamSlots
            .Where(slot => slot.GameId == gameId)
            .MaxAsync(slot => (int?)slot.SlotIndex, cancellationToken);
        return (maxSlotIndex ?? 0) + 1;
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
            var invitation = await _dbContext.GameTeamInvitations
                .FirstOrDefaultAsync(candidate => candidate.Id == command.InvitationId, cancellationToken);
            if (invitation is null)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvitationNotFound);
            }

            if (invitation.InvitedUserId != command.UserId
                || invitation.Status != TeamInvitationStatusValue.Pending)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvitationNotPending);
            }

            var utcNow = DateTime.UtcNow;
            invitation.Status = TeamInvitationStatusValue.Accepted;
            invitation.RespondedAtUtc = utcNow;

            GameTeam team;
            if (command.TeamId.HasValue)
            {
                var existingTeam = await _dbContext.GameTeams
                    .FirstOrDefaultAsync(candidate => candidate.Id == command.TeamId.Value, cancellationToken);
                if (existingTeam is null)
                {
                    return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
                }

                team = existingTeam;
                if (team.Status != TeamStatusValue.Forming || team.SlotId != command.SlotId)
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
                    SlotId = command.SlotId,
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
                JoinedAtUtc = DateTime.UtcNow
            }
        );
        team.UpdatedAtUtc = DateTime.UtcNow;
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
        invitation.RespondedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameRegistrationResult<bool>(true, true, GameRegistrationErrorCode.None);
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> LoadTeamResultAsync(
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var dto = await _reads.LoadTeamDtoAsync(teamId, cancellationToken);
        if (dto is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.OperationFailed);
        }

        return new GameRegistrationResult<RegistrationTeamDto>(true, dto, GameRegistrationErrorCode.None);
    }

    private static GameRegistrationResult<T> Fail<T>(GameRegistrationErrorCode error) =>
        new(false, default, error);
}
