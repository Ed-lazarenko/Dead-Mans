using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameBoardRepository
{
    public async Task<GameTeamQueueResult> GetCurrentTeamQueueAsync(
        CancellationToken cancellationToken = default
    )
    {
        var currentGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(
                x =>
                    !x.IsDeleted
                    && (x.Status == GameStatusValue.Active || x.Status == GameStatusValue.Ready)
            )
            .OrderByDescending(x => x.Status == GameStatusValue.Active)
            .ThenByDescending(x => x.StartedAtUtc ?? x.ReadyAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!currentGameId.HasValue)
        {
            return EmptyTeamQueueResult();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            currentGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return EmptyTeamQueueResult();
        }

        var teams = rosters
            .Select(roster =>
                new GameTeamQueueItem(
                    roster.TeamId,
                    roster.TeamName,
                    roster.TeamSlotIndex,
                    roster.IsPlayed,
                    roster.PlayedAtUtc,
                    roster.Participants
                        .Select(participant => new GameTeamQueueParticipant(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();

        var playedTeams = teams.Count(x => x.IsPlayed);
        return new GameTeamQueueResult(
            new GameTeamQueueSummary(
                teams.Length,
                playedTeams,
                Math.Max(teams.Length - playedTeams, 0)
            ),
            teams
        );
    }

    private static GameTeamQueueResult EmptyTeamQueueResult()
    {
        return new GameTeamQueueResult(
            new GameTeamQueueSummary(0, 0, 0),
            Array.Empty<GameTeamQueueItem>()
        );
    }

    public async Task<SetActiveGameTeamOutcome> SetActiveTeamAsync(
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var activeGame = await QueryCurrentActiveGames().FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return SetActiveGameTeamOutcome.NoActiveGame;
        }

        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {activeGame.Id} FOR UPDATE",
                cancellationToken
            );
            await _dbContext.Entry(activeGame).ReloadAsync(cancellationToken);
            if (activeGame.Status != GameStatusValue.Active || activeGame.IsDeleted)
            {
                return SetActiveGameTeamOutcome.NoActiveGame;
            }
        }

        if (activeGame.ActiveTeamId != teamId
            && await _dbContext.GameRounds.AnyAsync(
                round =>
                    round.GameId == activeGame.Id
                    && (round.Status == GameRoundStatusValue.AwaitingModifiers
                        || round.Status == GameRoundStatusValue.Preparing
                        || round.Status == GameRoundStatusValue.InProgress
                        || round.Status == GameRoundStatusValue.ReviewingResults),
                cancellationToken
            ))
        {
            return SetActiveGameTeamOutcome.RoundInProgress;
        }

        if (!teamId.HasValue)
        {
            activeGame.ActiveTeamId = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return SetActiveGameTeamOutcome.Updated;
        }

        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(candidate => candidate.Id == teamId.Value && candidate.GameId == activeGame.Id)
            .Select(
                candidate =>
                    new
                    {
                        candidate.Id,
                        candidate.Status,
                        candidate.IsPlayed,
                        candidate.DisbandedAtUtc,
                        ActiveMembersCount = candidate.Members.Count(member => member.LeftAtUtc == null),
                    }
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return SetActiveGameTeamOutcome.TeamNotFound;
        }

        if (team.Status != TeamStatusValue.Confirmed || team.DisbandedAtUtc != null)
        {
            return SetActiveGameTeamOutcome.TeamNotConfirmed;
        }

        if (team.IsPlayed)
        {
            return SetActiveGameTeamOutcome.TeamAlreadyPlayed;
        }

        if (team.ActiveMembersCount == 0)
        {
            return SetActiveGameTeamOutcome.TeamHasNoActiveMembers;
        }

        activeGame.ActiveTeamId = team.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return SetActiveGameTeamOutcome.Updated;
    }

    public async Task<SetGameTeamPlayedStateOutcome> SetGameTeamPlayedStateAsync(
        Guid teamId,
        bool isPlayed,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var activeGame = await QueryCurrentActiveGames().FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return SetGameTeamPlayedStateOutcome.NoActiveGame;
        }

        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {activeGame.Id} FOR UPDATE",
                cancellationToken
            );
            await _dbContext.Entry(activeGame).ReloadAsync(cancellationToken);
            if (activeGame.Status != GameStatusValue.Active || activeGame.IsDeleted)
            {
                return SetGameTeamPlayedStateOutcome.NoActiveGame;
            }
        }

        if (isPlayed && await _dbContext.GameRounds.AnyAsync(
                round =>
                    round.GameId == activeGame.Id
                    && (round.Status == GameRoundStatusValue.AwaitingModifiers
                        || round.Status == GameRoundStatusValue.Preparing
                        || round.Status == GameRoundStatusValue.InProgress
                        || round.Status == GameRoundStatusValue.ReviewingResults),
                cancellationToken
            ))
        {
            return SetGameTeamPlayedStateOutcome.RoundInProgress;
        }

        var team = await _dbContext.GameTeams.FirstOrDefaultAsync(
            candidate => candidate.Id == teamId && candidate.GameId == activeGame.Id,
            cancellationToken
        );
        if (team is null)
        {
            return SetGameTeamPlayedStateOutcome.TeamNotFound;
        }

        if (team.Status != TeamStatusValue.Confirmed || team.DisbandedAtUtc != null)
        {
            return SetGameTeamPlayedStateOutcome.TeamNotConfirmed;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        team.IsPlayed = isPlayed;
        team.PlayedAtUtc = isPlayed ? now : null;
        team.UpdatedAtUtc = now;

        if (isPlayed && activeGame.ActiveTeamId == team.Id)
        {
            activeGame.ActiveTeamId = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        return SetGameTeamPlayedStateOutcome.Updated;
    }

    public async Task<bool> CurrentActiveGameHasActiveTeamAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await QueryCurrentActiveGames()
            .AsNoTracking()
            .Select(game => new { game.Id, game.ActiveTeamId })
            .FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return true;
        }

        if (!activeGame.ActiveTeamId.HasValue)
        {
            return false;
        }

        return await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team =>
                    team.Id == activeGame.ActiveTeamId.Value
                    && team.GameId == activeGame.Id
                    && team.Status == TeamStatusValue.Confirmed
                    && !team.IsPlayed
                    && team.DisbandedAtUtc == null
            )
            .AnyAsync(team => team.Members.Any(member => member.LeftAtUtc == null), cancellationToken);
    }

    public async Task<bool> CurrentActiveGameHasActiveRoundAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await QueryCurrentActiveGames()
            .AsNoTracking()
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return false;
        }

        return await _dbContext.GameRounds.AnyAsync(
            round =>
                round.GameId == activeGameId.Value
                && (round.Status == GameRoundStatusValue.AwaitingModifiers
                    || round.Status == GameRoundStatusValue.Preparing
                    || round.Status == GameRoundStatusValue.InProgress
                    || round.Status == GameRoundStatusValue.ReviewingResults),
            cancellationToken
        );
    }

    private IQueryable<Game> QueryCurrentActiveGames()
    {
        return _dbContext.Games
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .OrderByDescending(game => game.StartedAtUtc ?? game.CreatedAtUtc);
    }
}
