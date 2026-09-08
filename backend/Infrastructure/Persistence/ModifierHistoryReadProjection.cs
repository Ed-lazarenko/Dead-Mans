using System.Text;
using backend.Application.Contracts;
using backend.Data;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal sealed class ModifierHistoryReadProjection
{
    private readonly ApplicationDbContext _dbContext;

    public ModifierHistoryReadProjection(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModifierHistoryPage<ModifierHistorySummary>> LoadHistoryAsync(
        ModifierHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var definitions = _dbContext.ModifierDefinitions.AsNoTracking()
            .Where(x => x.CurrentVersionId != null);
        definitions = query.Status switch
        {
            "active" => definitions.Where(x => !x.IsArchived),
            "archived" => definitions.Where(x => x.IsArchived),
            _ => definitions
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            definitions = _dbContext.Database.IsRelational()
                ? definitions.Where(x =>
                    EF.Functions.ILike(x.CurrentVersion!.Name, $"%{EscapeLikePattern(search)}%", "\\")
                    || EF.Functions.ILike(x.CurrentVersion.Category, $"%{EscapeLikePattern(search)}%", "\\"))
                : definitions.Where(x =>
                    x.CurrentVersion!.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.CurrentVersion.Category.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ));
        }
        if (ModifierHistoryCursor.TryDecode(query.Cursor, out var cursorAt, out var cursorId))
        {
            definitions = definitions.Where(x =>
                x.CreatedAtUtc < cursorAt
                || (x.CreatedAtUtc == cursorAt && x.Id.CompareTo(cursorId) < 0));
        }

        var rows = await definitions
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new ModifierHistorySummary(
                x.Id,
                x.CurrentVersion!.Revision,
                x.CurrentVersion.Name,
                x.CurrentVersion.Category,
                x.CurrentVersion.IconEmoji,
                x.CurrentVersion.ActivationCost,
                x.IsArchived,
                x.CreatedAtUtc,
                x.ArchivedAtUtc,
                _dbContext.ModifierDefinitionVersions.Count(v => v.ModifierId == x.Id),
                _dbContext.GameEnabledModifiers.Count(e => e.ModifierId == x.Id),
                _dbContext.GameModifierActivations.Count(a => a.ModifierId == x.Id)))
            .Take(query.Limit + 1)
            .ToArrayAsync(cancellationToken);
        var items = rows.Take(query.Limit).ToArray();
        var next = rows.Length > query.Limit && items.Length > 0
            ? ModifierHistoryCursor.Encode(items[^1].CreatedAtUtc, items[^1].ModifierId)
            : null;
        return new ModifierHistoryPage<ModifierHistorySummary>(items, next);
    }

    public async Task<ModifierHistoryPage<ModifierVersionSummary>?> LoadVersionsAsync(
        Guid modifierId,
        ModifierVersionQuery query,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.ModifierDefinitions.AsNoTracking()
            .AnyAsync(x => x.Id == modifierId, cancellationToken))
        {
            return null;
        }

        var versions = _dbContext.ModifierDefinitionVersions.AsNoTracking()
            .Where(x => x.ModifierId == modifierId);
        if (ModifierRevisionCursor.TryDecode(query.Cursor, out var beforeRevision))
        {
            versions = versions.Where(x => x.Revision < beforeRevision);
        }
        var rows = await versions
            .OrderByDescending(x => x.Revision)
            .ThenByDescending(x => x.Id)
            .Select(x => new ModifierVersionSummary(
                x.Id,
                x.ModifierId,
                x.Revision,
                x.Name,
                x.CreatedAtUtc,
                x.CreatedByUserId,
                x.CreatedByDisplayNameSnapshot,
                x.ChangeNote,
                x.ChangeType,
                x.CascadeSourceModifierId,
                x.ChangedFields))
            .Take(query.Limit + 1)
            .ToArrayAsync(cancellationToken);
        var items = rows.Take(query.Limit).ToArray();
        var next = rows.Length > query.Limit && items.Length > 0
            ? ModifierRevisionCursor.Encode(items[^1].Revision)
            : null;
        return new ModifierHistoryPage<ModifierVersionSummary>(items, next);
    }

    public async Task<ModifierVersionDetail?> LoadVersionAsync(
        Guid modifierId,
        int revision,
        CancellationToken cancellationToken)
    {
        var row = await _dbContext.ModifierDefinitionVersions.AsNoTracking()
            .Where(x => x.ModifierId == modifierId && x.Revision == revision)
            .Select(x => new { Version = x, x.Modifier.IsArchived, x.Modifier.CurrentVersionId })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var conflicts = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => x.ModifierVersionId == row.Version.Id)
            .OrderBy(x => x.ConflictingModifierNameSnapshot)
            .Select(x => new ModifierConflictSnapshot(
                x.ConflictingModifierId,
                x.ConflictingModifierNameSnapshot))
            .ToArrayAsync(cancellationToken);
        return new ModifierVersionDetail(
            row.Version.Id,
            modifierId,
            revision,
            row.Version.Name,
            row.Version.Description,
            row.Version.Category,
            row.Version.IconEmoji,
            row.Version.ActivationCommand,
            row.Version.ActivationCost,
            new GameModifierActivationLimit(row.Version.MaxActivationsPerRound),
            row.Version.NormalizedTags,
            ModifierBehaviorV2Json.Deserialize(row.Version.BehaviorV2Json),
            conflicts,
            row.Version.CreatedAtUtc,
            row.Version.CreatedByUserId,
            row.Version.CreatedByDisplayNameSnapshot,
            row.Version.ChangeNote,
            row.Version.ChangeType,
            row.Version.CascadeSourceModifierId,
            row.Version.ChangedFields,
            row.CurrentVersionId == row.Version.Id,
            row.IsArchived);
    }

    public async Task<ModifierHistoryPage<ModifierVersionGameSummary>?> LoadVersionGamesAsync(
        Guid modifierId,
        int revision,
        ModifierVersionQuery query,
        CancellationToken cancellationToken)
    {
        var versionId = await _dbContext.ModifierDefinitionVersions.AsNoTracking()
            .Where(x => x.ModifierId == modifierId && x.Revision == revision)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue)
        {
            return null;
        }

        var games = _dbContext.GameEnabledModifiers.AsNoTracking()
            .Where(x => x.ModifierVersionId == versionId.Value);
        if (ModifierHistoryCursor.TryDecode(query.Cursor, out var cursorAt, out var cursorId))
        {
            games = games.Where(x =>
                (x.Game.StartedAtUtc ?? x.Game.CreatedAtUtc) < cursorAt
                || ((x.Game.StartedAtUtc ?? x.Game.CreatedAtUtc) == cursorAt
                    && x.GameId.CompareTo(cursorId) < 0));
        }
        var rows = await games
            .OrderByDescending(x => x.Game.StartedAtUtc ?? x.Game.CreatedAtUtc)
            .ThenByDescending(x => x.GameId)
            .Select(x => new ModifierVersionGameSummary(
                x.GameId,
                x.Game.Title,
                x.Game.Status,
                x.Game.StartedAtUtc,
                x.Game.FinishedAtUtc,
                _dbContext.GameModifierActivations.Count(a =>
                    a.GameId == x.GameId
                    && a.ModifierVersionId == versionId.Value
                    && a.Status != GameModifierActivationStatusValue.Cancelled),
                _dbContext.GameModifierActivations.Count(a =>
                    a.GameId == x.GameId
                    && a.ModifierVersionId == versionId.Value
                    && a.Status == GameModifierActivationStatusValue.Cancelled),
                _dbContext.GameRoundModifierResults.Count(r =>
                    r.GameModifierActivation.GameId == x.GameId
                    && r.GameModifierActivation.ModifierVersionId == versionId.Value),
                x.EmergencyDisabledAtUtc != null))
            .Take(query.Limit + 1)
            .ToArrayAsync(cancellationToken);
        var items = rows.Take(query.Limit).ToArray();
        var next = rows.Length > query.Limit && items.Length > 0
            ? ModifierHistoryCursor.Encode(
                items[^1].StartedAtUtc ?? DateTime.MinValue,
                items[^1].GameId)
            : null;
        return new ModifierHistoryPage<ModifierVersionGameSummary>(items, next);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

internal static class ModifierHistoryCursor
{
    public static string Encode(DateTime at, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{at.Ticks}:{id:N}"));

    public static bool TryDecode(string? cursor, out DateTime at, out Guid id)
    {
        at = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':');
            return parts.Length == 2
                && long.TryParse(parts[0], out var ticks)
                && ticks >= DateTime.MinValue.Ticks
                && ticks <= DateTime.MaxValue.Ticks
                && Guid.TryParseExact(parts[1], "N", out id)
                && (at = new DateTime(ticks, DateTimeKind.Utc)) != default;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

internal static class ModifierRevisionCursor
{
    public static string Encode(int revision) =>
        Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                revision.ToString(System.Globalization.CultureInfo.InvariantCulture)
            )
        );

    public static bool TryDecode(string? cursor, out int revision)
    {
        revision = 0;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }
        try
        {
            return int.TryParse(
                    Encoding.UTF8.GetString(Convert.FromBase64String(cursor)),
                    out revision)
                && revision > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
