using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class GameLifecycleReadStore : IGameLifecycleReadStore
{
    private readonly ApplicationDbContext _dbContext;

    public GameLifecycleReadStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DraftGameLifecycleContext?> GetLatestDraftForOpenAsync(CancellationToken cancellationToken) =>
        _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Draft && !game.IsDeleted)
            .OrderByDescending(game => game.CreatedAtUtc)
            .Select(
                game => new DraftGameLifecycleContext(
                    game.Id,
                    game.MinPlayersPerTeam,
                    game.MaxPlayersPerTeam
                )
            )
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyReadyGameAsync(CancellationToken cancellationToken) =>
        _dbContext.Games.AnyAsync(
            game => game.Status == GameStatusValue.Ready && !game.IsDeleted,
            cancellationToken
        );

    public Task<bool> AnyActiveGameAsync(CancellationToken cancellationToken) =>
        _dbContext.Games.AnyAsync(
            game => game.Status == GameStatusValue.Active && !game.IsDeleted,
            cancellationToken
        );

    public Task<Guid?> GetReadyGameIdForStartAsync(CancellationToken cancellationToken) =>
        _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Ready && !game.IsDeleted)
            .OrderByDescending(game => game.ReadyAtUtc)
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<GameLifecycleErrorCode> GetStartValidationErrorAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .Where(candidate => candidate.Id == gameId && candidate.Status == GameStatusValue.Ready && !candidate.IsDeleted)
            .Select(candidate => new { candidate.MinPlayersPerTeam, candidate.MaxPlayersPerTeam })
            .FirstOrDefaultAsync(cancellationToken);
        if (game is null)
        {
            return GameLifecycleErrorCode.GameNotReady;
        }

        if (await _dbContext.GameTeams.AsNoTracking().AnyAsync(
                team => team.GameId == gameId && team.Status == TeamStatusValue.Forming,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.UnconfirmedTeams;
        }

        if (await _dbContext.GameParticipationInvitations.AsNoTracking().AnyAsync(
                invitation => invitation.GameId == gameId
                    && invitation.Status == ParticipationInvitationStatusValue.Pending,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.PendingInvitations;
        }

        if (await _dbContext.GameTeams.AsNoTracking().AnyAsync(
                team => team.GameId == gameId
                    && team.Status == TeamStatusValue.Confirmed
                    && team.DisbandRequestedAtUtc != null,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.PendingDisbandRequests;
        }

        var confirmedTeamIds = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(team => team.GameId == gameId && team.Status == TeamStatusValue.Confirmed)
            .Select(team => team.Id)
            .ToListAsync(cancellationToken);
        if (confirmedTeamIds.Count == 0)
        {
            return GameLifecycleErrorCode.NoConfirmedTeams;
        }

        var activeMemberCounts = await _dbContext.GameTeamMembers
            .AsNoTracking()
            .Where(member => member.GameId == gameId && member.LeftAtUtc == null)
            .GroupBy(member => member.TeamId)
            .Select(group => new { TeamId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TeamId, item => item.Count, cancellationToken);

        return confirmedTeamIds.Any(teamId =>
            !activeMemberCounts.TryGetValue(teamId, out var count)
            || count < game.MinPlayersPerTeam
            || count > game.MaxPlayersPerTeam
        )
            ? GameLifecycleErrorCode.InvalidConfirmedTeamRoster
            : GameLifecycleErrorCode.None;
    }

    public Task<Guid?> GetActiveGameIdForFinishAsync(CancellationToken cancellationToken) =>
        _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .OrderByDescending(game => game.StartedAtUtc)
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
