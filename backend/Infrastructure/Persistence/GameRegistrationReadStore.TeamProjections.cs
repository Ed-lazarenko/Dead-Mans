using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class GameRegistrationReadStore
{
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
        return teams.Count > 0 ? teams[0] : null;
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
