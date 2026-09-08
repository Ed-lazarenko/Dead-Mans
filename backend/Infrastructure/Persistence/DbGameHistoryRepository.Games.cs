using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Infrastructure.Configuration;
using backend.Domain.Persistence;
using backend.Domain.GameModifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameHistoryRepository : IGameHistoryRepository
{
    public async Task<IReadOnlyList<GameHistoryGameSummary>> GetGamesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var games = await _dbContext.Games
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(
                x =>
                    new GameRow(
                        x.Id,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc
                    )
            )
            .ToArrayAsync(cancellationToken);

        var roundCounts = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(
                x =>
                    !x.Game.IsDeleted
                    && x.Status == GameRoundStatusValue.Completed
            )
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var quizCounts = await _dbContext.GameQuizRounds
            .AsNoTracking()
            .Where(x => !x.Game!.IsDeleted)
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var manualQuizCounts = await _dbContext.GameQuizPointLedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.EntryType == GameQuizPointEntryTypeValue.ManualAdjustment
                && !x.Game.IsDeleted)
            .GroupBy(x => x.GameId)
            .Select(x => new CountRow(x.Key, x.Count()))
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);

        var uniquePlayers = await LoadUniquePlayerCountsByGameAsync(cancellationToken);

        return games
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(
                x =>
                    new GameHistoryGameSummary(
                        x.GameId,
                        x.Title,
                        x.Status,
                        x.CreatedAtUtc,
                        x.StartedAtUtc,
                        x.FinishedAtUtc,
                        roundCounts.GetValueOrDefault(x.GameId, 0),
                        quizCounts.GetValueOrDefault(x.GameId, 0)
                            + manualQuizCounts.GetValueOrDefault(x.GameId, 0),
                        uniquePlayers.GetValueOrDefault(x.GameId, 0)
                    )
            )
            .ToArray();
    }

}
