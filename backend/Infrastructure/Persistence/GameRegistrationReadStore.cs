using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;
public sealed class GameRegistrationReadStore : IGameRegistrationReadStore
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
        Guid slotId,
        CancellationToken cancellationToken
    ) =>
        _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.Id == slotId && slot.GameId == gameId)
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
        Guid slotId,
        CancellationToken cancellationToken = default
    ) =>
        await LoadTeamInviteTargetSnapshotAsync(
            _dbContext.GameTeams
                .AsNoTracking()
                .Where(
                    candidate => candidate.GameId == gameId
                        && candidate.SlotId == slotId
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

    public async Task<GameRegistrationSnapshot> BuildSnapshotAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .FirstAsync(x => x.Id == gameId && !x.IsDeleted, cancellationToken);

        var slots = await _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.GameId == gameId)
            .OrderBy(slot => slot.SlotIndex)
            .ToListAsync(cancellationToken);

        var teams = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team => team.GameId == gameId
                    && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            )
            .ToListAsync(cancellationToken);

        var teamDtos = await LoadTeamsDtoAsync(gameId, cancellationToken);
        var blockedSlotIds = await GetBlockedTeamSlotIdsAsync(gameId, cancellationToken);

        var myTeam = teamDtos.FirstOrDefault(
            team => team.Members.Any(member => member.Player.UserId == userId)
        );

        var slotDtos = new List<RegistrationTeamSlotDto>();
        foreach (var slot in slots)
        {
            var occupyingTeam = teams.FirstOrDefault(
                team => team.SlotId == slot.Id && TeamStatusValue.OccupiesSlot(team.Status)
            );
            var blocked = IGameRegistrationReadStore.IsSlotBlocked(slot.Id, blockedSlotIds);
            slotDtos.Add(
                new RegistrationTeamSlotDto(
                    slot.Id,
                    slot.SlotIndex,
                    slot.SlotType,
                    slot.ReservedLabel,
                    slot.SlotType == TeamSlotTypeValue.Public
                        && !blocked
                        && occupyingTeam is null,
                    occupyingTeam?.Id,
                    occupyingTeam?.Status
                )
            );
        }

        var myInvites = await (
            from invitation in _dbContext.GameTeamInvitations.AsNoTracking()
            join slot in _dbContext.GameTeamSlots.AsNoTracking() on invitation.SlotId equals slot.Id
            join invitedByUser in _dbContext.Users.AsNoTracking() on invitation.InvitedByUserId equals invitedByUser.Id into invitedByUsers
            from invitedByUser in invitedByUsers.DefaultIfEmpty()
            join invitedUser in _dbContext.Users.AsNoTracking() on invitation.InvitedUserId equals invitedUser.Id into invitedUsers
            from invitedUser in invitedUsers.DefaultIfEmpty()
            where invitation.GameId == gameId
                && invitation.InvitedUserId == userId
                && invitation.Status == TeamInvitationStatusValue.Pending
            select new RegistrationInvitationDto(
                invitation.Id,
                slot.Id,
                slot.SlotIndex,
                invitation.TeamId,
                invitation.Status,
                invitation.CreatedAtUtc,
                invitedByUser != null ? invitedByUser.DisplayName : null,
                invitedUser != null ? invitedUser.DisplayName : null
            )
        ).ToListAsync(cancellationToken);

        var myTeamEntity = myTeam is null
            ? null
            : await _dbContext.GameTeams
                .AsNoTracking()
                .Where(team => team.Id == myTeam.TeamId)
                .Select(team => new { team.Id, team.CreatedByUserId, team.RecruitmentOpen, team.Status })
                .FirstOrDefaultAsync(cancellationToken);

        IReadOnlyList<RegistrationInvitationDto> myOutgoingInvitations = myTeamEntity is null
            ? []
            : await (
                from invitation in _dbContext.GameTeamInvitations.AsNoTracking()
                join slot in _dbContext.GameTeamSlots.AsNoTracking() on invitation.SlotId equals slot.Id
                join invitedByUser in _dbContext.Users.AsNoTracking() on invitation.InvitedByUserId equals invitedByUser.Id into invitedByUsers
                from invitedByUser in invitedByUsers.DefaultIfEmpty()
                join invitedUser in _dbContext.Users.AsNoTracking() on invitation.InvitedUserId equals invitedUser.Id into invitedUsers
                from invitedUser in invitedUsers.DefaultIfEmpty()
                where invitation.GameId == gameId
                    && invitation.TeamId == myTeamEntity.Id
                    && invitation.Status == TeamInvitationStatusValue.Pending
                select new RegistrationInvitationDto(
                    invitation.Id,
                    slot.Id,
                    slot.SlotIndex,
                    invitation.TeamId,
                    invitation.Status,
                    invitation.CreatedAtUtc,
                    invitedByUser != null ? invitedByUser.DisplayName : null,
                    invitedUser != null ? invitedUser.DisplayName : null
                )
            ).ToListAsync(cancellationToken);

        var canInvitePlayersToMyTeam = myTeamEntity is not null
            && myTeamEntity.CreatedByUserId == userId
            && !myTeamEntity.RecruitmentOpen
            && myTeamEntity.Status == TeamStatusValue.Forming
            && (myTeam?.Members.Count ?? 0) < game.MaxPlayersPerTeam
            && myOutgoingInvitations.Count == 0;

        IReadOnlyList<RegistrationPlayerDto> invitablePlayers = canInvitePlayersToMyTeam
            ? await _dbContext.Users
                .AvailableForGameRegistration(_dbContext, gameId, excludedUserId: userId)
                .Select(user => new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName))
                .ToListAsync(cancellationToken)
            : [];

        return new GameRegistrationSnapshot(
            gameId,
            game.Status,
            game.MinPlayersPerTeam,
            game.MaxPlayersPerTeam,
            slotDtos,
            teamDtos,
            myTeam,
            myInvites,
            myOutgoingInvitations,
            canInvitePlayersToMyTeam,
            invitablePlayers
        );
    }

    public async Task<GameRegistrationAdminSnapshot> BuildAdminSnapshotAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .FirstAsync(x => x.Id == gameId && !x.IsDeleted, cancellationToken);

        var slotDtos = await BuildSlotDtosAsync(gameId, cancellationToken);
        var teamDtos = await LoadTeamsDtoAsync(gameId, cancellationToken);
        var availablePlayers = await _dbContext.Users
            .AvailableForGameRegistration(_dbContext, gameId)
            .Select(user => new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName))
            .ToListAsync(cancellationToken);

        return new GameRegistrationAdminSnapshot(
            gameId,
            game.Status,
            game.MinPlayersPerTeam,
            game.MaxPlayersPerTeam,
            BuildLaunchSummary(teamDtos, game.MinPlayersPerTeam, game.MaxPlayersPerTeam),
            slotDtos,
            teamDtos,
            availablePlayers
        );
    }

    private static GameRegistrationLaunchSummary BuildLaunchSummary(
        IReadOnlyList<RegistrationTeamDto> teams,
        short minPlayersPerTeam,
        short maxPlayersPerTeam
    )
    {
        var confirmedTeamsCount = teams.Count(team => team.Status == TeamStatusValue.Confirmed);
        var formingTeamsCount = teams.Count(team => team.Status == TeamStatusValue.Forming);
        var pendingInvitationsCount = teams.Sum(team => team.PendingInvitations.Count);
        var disbandRequestsCount = teams.Count(team => team.DisbandRequestedAtUtc is not null);
        var invalidConfirmedRostersCount = teams.Count(
            team =>
                team.Status == TeamStatusValue.Confirmed
                && (team.Members.Count < minPlayersPerTeam || team.Members.Count > maxPlayersPerTeam)
        );

        return new GameRegistrationLaunchSummary(
            confirmedTeamsCount > 0
            && formingTeamsCount == 0
            && pendingInvitationsCount == 0
            && disbandRequestsCount == 0
            && invalidConfirmedRostersCount == 0,
            confirmedTeamsCount,
            formingTeamsCount,
            pendingInvitationsCount,
            disbandRequestsCount,
            invalidConfirmedRostersCount
        );
    }

    public async Task<RegistrationTeamDto?> LoadTeamDtoAsync(
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var gameId = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(team => team.Id == teamId)
            .Select(team => team.GameId)
            .FirstOrDefaultAsync(cancellationToken);
        if (gameId == Guid.Empty)
        {
            return null;
        }

        var teams = await LoadTeamsDtoAsync(gameId, cancellationToken, [teamId]);
        return teams.FirstOrDefault();
    }

    private async Task<IReadOnlyList<RegistrationTeamDto>> LoadTeamsDtoAsync(
        Guid gameId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? teamIds = null
    )
    {
        var teamsQuery = _dbContext.GameTeams
            .AsNoTracking()
            .Include(team => team.Slot)
            .Where(
                team => team.GameId == gameId
                    && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            );

        if (teamIds is { Count: > 0 })
        {
            teamsQuery = teamsQuery.Where(team => teamIds.Contains(team.Id));
        }

        var teams = await teamsQuery.ToListAsync(cancellationToken);
        if (teams.Count == 0)
        {
            return Array.Empty<RegistrationTeamDto>();
        }

        var loadedTeamIds = teams.Select(team => team.Id).ToList();
        var membersByTeamId = await LoadMembersByTeamIdAsync(loadedTeamIds, cancellationToken);
        var pendingInvitationsByTeamId = await LoadPendingInvitationsByTeamIdAsync(
            loadedTeamIds,
            cancellationToken
        );
        var disbandRequestUserIds = teams
            .Select(team => team.DisbandRequestedByUserId)
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();
        var disbandRequestNamesByUserId = disbandRequestUserIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(user => disbandRequestUserIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var activeTeamId = await _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Id == gameId && game.Status == GameStatusValue.Active && !game.IsDeleted)
            .Select(game => game.ActiveTeamId)
            .FirstOrDefaultAsync(cancellationToken);

        return teams
            .Where(team => team.Slot is not null)
            .Select(team =>
            {
                membersByTeamId.TryGetValue(team.Id, out var members);
                pendingInvitationsByTeamId.TryGetValue(team.Id, out var pendingInvitations);
                var disbandRequestedByDisplayName = team.DisbandRequestedByUserId.HasValue
                    && disbandRequestNamesByUserId.TryGetValue(
                        team.DisbandRequestedByUserId.Value,
                        out var displayName
                    )
                        ? displayName
                        : null;
                return MapTeamDto(
                    team,
                    members ?? (IReadOnlyList<RegistrationTeamMemberDto>)[],
                    pendingInvitations ?? (IReadOnlyList<RegistrationTeamPendingInvitationDto>)[],
                    disbandRequestedByDisplayName,
                    activeTeamId == team.Id
                );
            })
            .ToList();
    }

    public async Task<IReadOnlyList<RegistrationTeamDto>> LoadTeamsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken
    ) => await LoadTeamsDtoAsync(gameId, cancellationToken);

    private async Task<IReadOnlyList<RegistrationTeamSlotDto>> BuildSlotDtosAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var slots = await _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.GameId == gameId)
            .OrderBy(slot => slot.SlotIndex)
            .ToListAsync(cancellationToken);

        var teams = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team => team.GameId == gameId
                    && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            )
            .ToListAsync(cancellationToken);

        var blockedSlotIds = await GetBlockedTeamSlotIdsAsync(gameId, cancellationToken);
        var slotDtos = new List<RegistrationTeamSlotDto>(slots.Count);
        foreach (var slot in slots)
        {
            var occupyingTeam = teams.FirstOrDefault(
                team => team.SlotId == slot.Id && TeamStatusValue.OccupiesSlot(team.Status)
            );
            var blocked = IGameRegistrationReadStore.IsSlotBlocked(slot.Id, blockedSlotIds);
            slotDtos.Add(
                new RegistrationTeamSlotDto(
                    slot.Id,
                    slot.SlotIndex,
                    slot.SlotType,
                    slot.ReservedLabel,
                    slot.SlotType == TeamSlotTypeValue.Public
                        && !blocked
                        && occupyingTeam is null,
                    occupyingTeam?.Id,
                    occupyingTeam?.Status
                )
            );
        }

        return slotDtos;
    }

    private async Task<TeamInviteTargetSnapshot?> LoadTeamInviteTargetSnapshotAsync(
        IQueryable<GameTeam> teamsQuery,
        CancellationToken cancellationToken
    )
    {
        var team = await teamsQuery
            .Select(
                candidate => new TeamInviteTargetRow(
                    candidate.Id,
                    candidate.SlotId,
                    candidate.Status,
                    candidate.RecruitmentOpen,
                    candidate.CreatedByUserId
                )
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return null;
        }

        var memberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == team.TeamId && member.LeftAtUtc == null,
            cancellationToken
        );
        var pendingInvitationCount = await _dbContext.GameTeamInvitations.CountAsync(
            invitation =>
                invitation.TeamId == team.TeamId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );

        return new TeamInviteTargetSnapshot(
            team.TeamId,
            team.SlotId,
            team.Status,
            memberCount,
            pendingInvitationCount,
            team.RecruitmentOpen,
            team.CreatedByUserId
        );
    }

    private async Task<Dictionary<Guid, List<RegistrationTeamMemberDto>>> LoadMembersByTeamIdAsync(
        IReadOnlyCollection<Guid> teamIds,
        CancellationToken cancellationToken
    )
    {
        var members = await _dbContext.GameTeamMembers
            .AsNoTracking()
            .Where(member => teamIds.Contains(member.TeamId) && member.LeftAtUtc == null)
            .Join(
                _dbContext.Users,
                member => member.UserId,
                user => user.Id,
                (member, user) =>
                    new
                    {
                        member.TeamId,
                        Dto = new RegistrationTeamMemberDto(
                            new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName),
                            member.JoinedAtUtc
                        )
                    }
            )
            .ToListAsync(cancellationToken);

        return members
            .GroupBy(member => member.TeamId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(member => member.Dto.JoinedAtUtc).Select(member => member.Dto).ToList()
            );
    }

    private async Task<Dictionary<Guid, List<RegistrationTeamPendingInvitationDto>>> LoadPendingInvitationsByTeamIdAsync(
        IReadOnlyCollection<Guid> teamIds,
        CancellationToken cancellationToken
    )
    {
        var invitations = await _dbContext.GameTeamInvitations
            .AsNoTracking()
            .Where(
                invitation => invitation.TeamId.HasValue
                    && teamIds.Contains(invitation.TeamId.Value)
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .Join(
                _dbContext.Users.AsNoTracking(),
                invitation => invitation.InvitedUserId,
                user => user.Id,
                (invitation, user) =>
                    new
                    {
                        TeamId = invitation.TeamId!.Value,
                        Dto = new RegistrationTeamPendingInvitationDto(
                            invitation.Id,
                            new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName),
                            invitation.CreatedAtUtc
                        )
                    }
            )
            .ToListAsync(cancellationToken);

        return invitations
            .GroupBy(invitation => invitation.TeamId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(invitation => invitation.Dto.CreatedAtUtc).Select(invitation => invitation.Dto).ToList()
            );
    }

    private static RegistrationTeamDto MapTeamDto(
        GameTeam team,
        IReadOnlyList<RegistrationTeamMemberDto> members,
        IReadOnlyList<RegistrationTeamPendingInvitationDto> pendingInvitations,
        string? disbandRequestedByDisplayName,
        bool isActiveInGame
    ) =>
        new(
            team.Id,
            team.Name,
            team.Slot!.SlotIndex,
            team.Slot.SlotType,
            team.Slot.ReservedLabel,
            team.RecruitmentOpen,
            team.Status,
            team.IsPlayed,
            team.DisbandRequestedAtUtc,
            team.DisbandRequestedByUserId,
            disbandRequestedByDisplayName,
            isActiveInGame,
            members,
            pendingInvitations
        );

    private sealed record TeamInviteTargetRow(
        Guid TeamId,
        Guid SlotId,
        string Status,
        bool RecruitmentOpen,
        Guid? CreatedByUserId
    );
}
