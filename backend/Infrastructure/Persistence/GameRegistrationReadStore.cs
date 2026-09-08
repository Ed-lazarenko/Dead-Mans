using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class GameRegistrationReadStore : IGameRegistrationReadStore
{
    private readonly ApplicationDbContext _dbContext;

    public GameRegistrationReadStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReadyGameRegistrationContext?> GetReadyGameAsync(CancellationToken cancellationToken) =>
        await _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Ready && !game.IsDeleted)
            .OrderByDescending(game => game.ReadyAtUtc)
            .Select(
                game => new ReadyGameRegistrationContext(
                    game.Id,
                    game.MinPlayersPerTeam,
                    game.MaxPlayersPerTeam
                )
            )
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ReadyGameRegistrationContext?> GetManageableGameAsync(
        CancellationToken cancellationToken
    ) =>
        await _dbContext.Games
            .AsNoTracking()
            .Where(
                game =>
                    !game.IsDeleted
                    && (game.Status == GameStatusValue.Active || game.Status == GameStatusValue.Ready)
            )
            .OrderByDescending(game => game.Status == GameStatusValue.Active)
            .ThenByDescending(game => game.StartedAtUtc ?? game.ReadyAtUtc ?? game.CreatedAtUtc)
            .Select(
                game => new ReadyGameRegistrationContext(
                    game.Id,
                    game.MinPlayersPerTeam,
                    game.MaxPlayersPerTeam
                )
            )
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> UserHasTeamMembershipAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        (
            from member in _dbContext.GameTeamMembers
            join team in _dbContext.GameTeams on member.TeamId equals team.Id
            where member.GameId == gameId
                && member.UserId == userId
                && member.LeftAtUtc == null
                && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            select member
        ).AnyAsync(cancellationToken);

    public Task<bool> HasPendingInvitationAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.GameId == gameId
                && invitation.InvitedUserId == userId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );

    public Task<PendingInvitationSnapshot?> GetPendingInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.GameTeamInvitations
            .AsNoTracking()
            .Where(
                invitation =>
                    invitation.Id == invitationId
                    && invitation.InvitedUserId == userId
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .Select(
                invitation => new PendingInvitationSnapshot(
                    invitation.Id,
                    invitation.GameId,
                    invitation.SlotId,
                    invitation.TeamId,
                    invitation.Status,
                    invitation.InvitedUserId
                )
            )
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<AvailableTeamSlot?> FindAvailablePublicSlotAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var blockedSlotIds = await GetBlockedTeamSlotIdsAsync(gameId, cancellationToken);
        var publicSlots = await _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.GameId == gameId && slot.SlotType == TeamSlotTypeValue.Public)
            .OrderBy(slot => slot.SlotIndex)
            .ToListAsync(cancellationToken);

        var slot = publicSlots.FirstOrDefault(candidate => !blockedSlotIds.Contains(candidate.Id));
        return slot is null
            ? null
            : new AvailableTeamSlot(slot.Id, slot.SlotIndex);
    }

    public async Task<HashSet<Guid>> GetBlockedTeamSlotIdsAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var occupyingSlotIds = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team => team.GameId == gameId
                    && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            )
            .Select(team => team.SlotId)
            .ToListAsync(cancellationToken);

        var pendingInviteSlotIds = await _dbContext.GameTeamInvitations
            .AsNoTracking()
            .Where(
                invitation =>
                    invitation.GameId == gameId
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .Select(invitation => invitation.SlotId)
            .ToListAsync(cancellationToken);

        var blocked = new HashSet<Guid>(occupyingSlotIds);
        blocked.UnionWith(pendingInviteSlotIds);
        return blocked;
    }

    public async Task<JoinableTeamSnapshot?> GetJoinableTeamAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(candidate => candidate.Id == teamId && candidate.GameId == gameId)
            .Select(
                candidate => new JoinableTeamSnapshot(
                    candidate.Id,
                    candidate.Status,
                    candidate.RecruitmentOpen
                )
            )
            .FirstOrDefaultAsync(cancellationToken);

        return team;
    }

    public async Task<TeamAdminActionSnapshot?> GetTeamAdminActionSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(candidate => candidate.Id == teamId && candidate.GameId == gameId)
            .Select(candidate => new { candidate.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return null;
        }

        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == teamId && member.LeftAtUtc == null,
            cancellationToken
        );

        return new TeamAdminActionSnapshot(team.Status, memberCount);
    }

    public async Task<TeamAdminLifecycleSnapshot?> GetTeamAdminLifecycleSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(candidate => candidate.Id == teamId && candidate.GameId == gameId)
            .Select(candidate => new { candidate.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return null;
        }

        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == teamId && member.LeftAtUtc == null,
            cancellationToken
        );
        var isActiveInGame = await _dbContext.Games.AnyAsync(
            game =>
                game.Id == gameId
                && game.Status == GameStatusValue.Active
                && game.ActiveTeamId == teamId
                && !game.IsDeleted,
            cancellationToken
        );

        return new TeamAdminLifecycleSnapshot(team.Status, memberCount, isActiveInGame);
    }

    public async Task<TeamInviteTargetSnapshot?> GetTeamInviteTargetSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken
    ) =>
        await LoadTeamInviteTargetSnapshotAsync(
            _dbContext.GameTeams
                .AsNoTracking()
                .Where(candidate => candidate.Id == teamId && candidate.GameId == gameId),
            cancellationToken
        );

    public Task<TeamSlotSnapshot?> GetTeamSlotAsync(
        Guid gameId,
        Guid teamSlotId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.Id == teamSlotId && slot.GameId == gameId)
            .Select(slot => new TeamSlotSnapshot(slot.Id, slot.SlotIndex))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetActiveTeamIdForUserAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await (
            from member in _dbContext.GameTeamMembers.AsNoTracking()
            join team in _dbContext.GameTeams.AsNoTracking() on member.TeamId equals team.Id
            where member.GameId == gameId
                && member.UserId == userId
                && member.LeftAtUtc == null
                && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            select (Guid?)team.Id
        ).FirstOrDefaultAsync(cancellationToken);

    public async Task<TeamInviteTargetSnapshot?> GetTeamBySlotAsync(
        Guid gameId,
        Guid teamSlotId,
        CancellationToken cancellationToken = default
    ) =>
        await LoadTeamInviteTargetSnapshotAsync(
            _dbContext.GameTeams
                .AsNoTracking()
                .Where(
                    candidate => candidate.GameId == gameId
                        && candidate.SlotId == teamSlotId
                        && (candidate.Status == TeamStatusValue.Forming
                            || candidate.Status == TeamStatusValue.Confirmed)
                ),
            cancellationToken
        );

    public Task<bool> ActiveUserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Users.AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

    public Task<bool> TeamHasPendingInvitationAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    ) =>
        _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.GameId == gameId
                && invitation.TeamId == teamId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
}
