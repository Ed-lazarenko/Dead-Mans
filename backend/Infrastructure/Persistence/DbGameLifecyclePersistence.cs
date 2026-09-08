using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameLifecyclePersistence : IGameLifecyclePersistence
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbGameLifecyclePersistence> _logger;
    private readonly TimeProvider _timeProvider;

    public DbGameLifecyclePersistence(
        ApplicationDbContext dbContext,
        ILogger<DbGameLifecyclePersistence> logger,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<GameLifecycleResult> OpenRegistrationAsync(
        Guid draftGameId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await ModifierCatalogTransactionLock.AcquireAsync(_dbContext, cancellationToken);

        var draft = await _dbContext.Games
            .Include(game => game.TeamSlots)
            .FirstOrDefaultAsync(
                game => game.Id == draftGameId && !game.IsDeleted,
                cancellationToken
            );
        if (draft is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.DraftNotFound);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {draft.Id} FOR UPDATE""",
                cancellationToken
            );
            await _dbContext.Entry(draft).ReloadAsync(cancellationToken);
        }

        if (draft.IsDeleted || draft.Status != GameStatusValue.Draft)
        {
            var error = draft.Status == GameStatusValue.Ready
                ? GameLifecycleErrorCode.CurrentGameAlreadyExists
                : GameLifecycleErrorCode.DraftNotFound;
            return new GameLifecycleResult(false, draft.Id, error);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        await GameTeamSlotInitializer.EnsureDefaultSlotsAsync(
            _dbContext,
            draft.Id,
            utcNow,
            cancellationToken
        );

        if (!await _dbContext.GameTeamSlots.AnyAsync(
                slot => slot.GameId == draft.Id,
                cancellationToken
            ))
        {
            return new GameLifecycleResult(
                false,
                draft.Id,
                GameLifecycleErrorCode.NoTeamSlots
            );
        }

        await LockQuestionCatalogForPublicationAsync(draft.Id, cancellationToken);

        var enabledModifiers = await _dbContext.GameEnabledModifiers
            .Include(item => item.ModifierDefinition)
            .Where(item => item.GameId == draft.Id)
            .ToArrayAsync(cancellationToken);
        if (enabledModifiers.Any(item =>
                item.ModifierDefinition.IsArchived
                || !item.ModifierDefinition.CurrentVersionId.HasValue))
        {
            return new GameLifecycleResult(
                false,
                draft.Id,
                GameLifecycleErrorCode.ModifierVersionBindingMissing
            );
        }

        foreach (var enabledModifier in enabledModifiers)
        {
            enabledModifier.ModifierVersionId =
                enabledModifier.ModifierDefinition.CurrentVersionId;
            enabledModifier.VersionPinnedAtUtc = utcNow;
        }

        var enabledQuestions = await _dbContext.GameEnabledQuestions
            .Include(item => item.QuestionDefinition)
            .ThenInclude(question => question.CategoryDefinition)
            .Include(item => item.QuestionDefinition)
            .ThenInclude(question => question.AcceptedAnswers)
            .Where(item => item.GameId == draft.Id)
            .ToArrayAsync(cancellationToken);
        foreach (var enabledQuestion in enabledQuestions)
        {
            var question = enabledQuestion.QuestionDefinition;
            enabledQuestion.QuestionRevisionSnapshot = question.Revision;
            enabledQuestion.QuestionCodeSnapshot = question.ExternalCode;
            enabledQuestion.CategoryNameSnapshot = question.CategoryDefinition?.Name ?? string.Empty;
            enabledQuestion.QuestionTextSnapshot = question.Text;
            enabledQuestion.AcceptedAnswersSnapshot = question.AcceptedAnswers
                .OrderByDescending(answer => answer.IsPrimary)
                .ThenBy(answer => answer.SortOrder)
                .Select(answer => answer.AnswerText)
                .ToArray();
            enabledQuestion.NormalizedAnswersSnapshot = question.AcceptedAnswers
                .OrderByDescending(answer => answer.IsPrimary)
                .ThenBy(answer => answer.SortOrder)
                .Select(answer => answer.NormalizedAnswer)
                .ToArray();
            enabledQuestion.RewardSnapshot = question.Reward;
            enabledQuestion.PrioritySnapshot = question.Priority;
            enabledQuestion.SnapshotAtUtc = utcNow;
        }

        draft.Status = GameStatusValue.Ready;
        draft.ReadyAtUtc = utcNow;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out var constraintName))
        {
            _logger.LogWarning(ex, "Open registration failed due to unique constraint {Constraint}.", constraintName);
            return MapLifecycleUniqueViolation(
                constraintName,
                draft.Id,
                GameLifecycleErrorCode.CurrentGameAlreadyExists
            );
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation("Game {GameId} moved to ready for registration.", draft.Id);
        return new GameLifecycleResult(true, draft.Id, GameLifecycleErrorCode.None);
    }

    public async Task<GameLifecycleResult> StartGameAsync(
        Guid readyGameId,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // Keep the active-game content-lock decision serializable with catalog edits/archive.
        // The revision itself was already pinned when the game became ready.
        await ModifierCatalogTransactionLock.AcquireAsync(_dbContext, cancellationToken);

        var ready = await _dbContext.Games
            .FirstOrDefaultAsync(
                game => game.Id == readyGameId && !game.IsDeleted,
                cancellationToken
            );
        if (ready is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.GameNotReady);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {ready.Id} FOR UPDATE""",
                cancellationToken
            );
            await _dbContext.Entry(ready).ReloadAsync(cancellationToken);
        }

        if (ready.IsDeleted || ready.Status != GameStatusValue.Ready)
        {
            return new GameLifecycleResult(false, ready.Id, GameLifecycleErrorCode.GameNotReady);
        }

        var validationError = await ValidateGameCanStartAsync(readyGameId, cancellationToken);
        if (validationError != GameLifecycleErrorCode.None)
        {
            return new GameLifecycleResult(false, ready.Id, validationError);
        }

        if (await _dbContext.GameEnabledModifiers.AnyAsync(
                item => item.GameId == readyGameId
                    && (item.ModifierVersionId == null || item.VersionPinnedAtUtc == null),
                cancellationToken))
        {
            return new GameLifecycleResult(
                false,
                ready.Id,
                GameLifecycleErrorCode.ModifierVersionBindingMissing
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        ready.Status = GameStatusValue.Active;
        ready.StartedAtUtc = now;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresUniqueViolation.TryGetConstraintName(ex, out var constraintName))
        {
            _logger.LogWarning(ex, "Start game failed due to unique constraint {Constraint}.", constraintName);
            return MapLifecycleUniqueViolation(constraintName, ready.Id, GameLifecycleErrorCode.ActiveGameAlreadyExists);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        _logger.LogInformation("Game {GameId} started.", ready.Id);
        return new GameLifecycleResult(true, ready.Id, GameLifecycleErrorCode.None);
    }

    public async Task<GameLifecycleResult> ArchiveGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _dbContext.Games.FirstOrDefaultAsync(
            candidate => candidate.Id == gameId && !candidate.IsDeleted,
            cancellationToken
        );
        if (game is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.GameNotFound);
        }

        if (game.Status == GameStatusValue.Draft)
        {
            return new GameLifecycleResult(
                false,
                game.Id,
                GameLifecycleErrorCode.DraftDeleteNotAllowed
            );
        }

        if (game.Status != GameStatusValue.Finished)
        {
            return new GameLifecycleResult(
                false,
                game.Id,
                GameLifecycleErrorCode.GameArchiveNotAllowed
            );
        }

        game.IsDeleted = true;
        game.DeletedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} archived (soft-delete).", game.Id);
        return new GameLifecycleResult(true, game.Id, GameLifecycleErrorCode.None);
    }

    private static GameLifecycleResult MapLifecycleUniqueViolation(
        string? constraintName,
        Guid gameId,
        GameLifecycleErrorCode fallbackConflict
    ) =>
        constraintName switch
        {
            PostgresUniqueViolation.GamesSingleCurrent => new GameLifecycleResult(
                false, gameId, fallbackConflict),
            _ => new GameLifecycleResult(false, gameId, fallbackConflict)
        };

    private async Task LockQuestionCatalogForPublicationAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        if (!_dbContext.Database.IsRelational())
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT question.id
            FROM question_definitions question
            JOIN game_enabled_questions enabled ON enabled.question_id = question.id
            WHERE enabled.game_id = {gameId}
            ORDER BY question.id
            FOR UPDATE OF question
            """,
            cancellationToken
        );
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT category.id
            FROM question_categories category
            JOIN question_definitions question ON question.category_id = category.id
            JOIN game_enabled_questions enabled ON enabled.question_id = question.id
            WHERE enabled.game_id = {gameId}
            ORDER BY category.id
            FOR SHARE OF category
            """,
            cancellationToken
        );
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            SELECT answer.id
            FROM question_accepted_answers answer
            JOIN game_enabled_questions enabled ON enabled.question_id = answer.question_id
            WHERE enabled.game_id = {gameId}
            ORDER BY answer.id
            FOR SHARE OF answer
            """,
            cancellationToken
        );
    }

    private async Task<GameLifecycleErrorCode> ValidateGameCanStartAsync(
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var game = await _dbContext.Games
            .Where(candidate => candidate.Id == gameId && candidate.Status == GameStatusValue.Ready && !candidate.IsDeleted)
            .Select(candidate => new { candidate.MinPlayersPerTeam, candidate.MaxPlayersPerTeam })
            .FirstOrDefaultAsync(cancellationToken);
        if (game is null)
        {
            return GameLifecycleErrorCode.GameNotReady;
        }

        if (await _dbContext.GameTeams.AnyAsync(
                team => team.GameId == gameId && team.Status == TeamStatusValue.Forming,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.UnconfirmedTeams;
        }

        if (await _dbContext.GameTeamInvitations.AnyAsync(
                invitation => invitation.GameId == gameId
                    && invitation.Status == TeamInvitationStatusValue.Pending,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.PendingInvitations;
        }

        if (await _dbContext.GameTeams.AnyAsync(
                team => team.GameId == gameId
                    && team.Status == TeamStatusValue.Confirmed
                    && team.DisbandRequestedAtUtc != null,
                cancellationToken
            ))
        {
            return GameLifecycleErrorCode.PendingDisbandRequests;
        }

        var confirmedTeamIds = await _dbContext.GameTeams
            .Where(team => team.GameId == gameId && team.Status == TeamStatusValue.Confirmed)
            .Select(team => team.Id)
            .ToListAsync(cancellationToken);
        if (confirmedTeamIds.Count == 0)
        {
            return GameLifecycleErrorCode.NoConfirmedTeams;
        }

        var activeMemberCounts = await _dbContext.GameTeamMembers
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
}
