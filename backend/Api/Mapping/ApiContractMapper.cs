using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static partial class ApiContractMapper
{
    public static AuthSessionDto ToDto(this AuthSession session)
    {
        return new AuthSessionDto(
            session.UserId,
            session.DisplayName,
            session.Roles
                .Select(TryMapAuthRole)
                .Where(role => role.HasValue)
                .Select(role => role!.Value)
                .ToArray()
        );
    }

    public static GameSetupDraftUpdate ToUpdateModel(this UpdateGameSetupRequestDto request)
    {
        return new GameSetupDraftUpdate(
            request.ExpectedVersion,
            request.Title,
            request.RowLabels,
            request.ColLabels,
            request.Cells
                .Select(cell => new GameSetupCellUpdate(cell.Id, cell.Row, cell.Col, cell.Title, cell.Cost))
                .ToArray(),
            (request.EnabledModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            (request.EnabledQuestionIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray()
        );
    }

    public static GameSetupSnapshotDto ToSetupDto(this GameBoardSnapshot snapshot)
    {
        return new GameSetupSnapshotDto(
            snapshot.GameId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.Version,
            snapshot.Rows,
            snapshot.Cols,
            snapshot.RowLabels.ToArray(),
            snapshot.ColLabels.ToArray(),
            snapshot.Cells.Select(ToDto).ToArray(),
            snapshot.EnabledModifierIds.Select(id => id.ToString()).ToArray(),
            snapshot.EnabledQuestionIds.ToArray()
        );
    }

    public static GameBoardCellMediaDto ToDto(this GameBoardCellMedia media)
    {
        return new GameBoardCellMediaDto(media.Url);
    }

    public static GameBoardSnapshotDto ToDto(this GameBoardSnapshot snapshot)
    {
        return new GameBoardSnapshotDto(
            snapshot.GameId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.Version,
            snapshot.Rows,
            snapshot.Cols,
            snapshot.RowLabels.ToArray(),
            snapshot.ColLabels.ToArray(),
            snapshot.Cells.Select(ToDto).ToArray(),
            snapshot.EnabledModifierIds.Select(id => id.ToString()).ToArray(),
            snapshot.ActiveModifiers.Select(ToDto).ToArray(),
            snapshot.ActiveTeamId
        );
    }

    public static GameUserNotificationDto ToDto(this GameUserNotification notification)
    {
        return new GameUserNotificationDto(
            notification.NotificationId.ToString(),
            notification.Type,
            notification.CreatedAtUtc,
            notification.ModifierName,
            notification.ActorDisplayName,
            notification.QuizPointsDelta
        );
    }

    public static GameTeamQueueItemDto ToDto(this GameTeamQueueItem item)
    {
        return new GameTeamQueueItemDto(
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.IsPlayed,
            item.PlayedAtUtc,
            item.Participants
                .Select(
                    participant =>
                        new GameTeamQueueParticipantDto(
                            participant.UserId.ToString(),
                            participant.DisplayName
                        )
                )
                .ToArray()
        );
    }

    public static GameTeamQueueSummaryDto ToDto(this GameTeamQueueSummary summary)
    {
        return new GameTeamQueueSummaryDto(
            summary.TotalTeams,
            summary.PlayedTeams,
            summary.RemainingTeams
        );
    }

    public static GameTeamQueueResultDto ToDto(this GameTeamQueueResult result)
    {
        return new GameTeamQueueResultDto(
            result.Summary.ToDto(),
            result.Teams.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameCellOpenedEventDto ToDto(this GameCellOpenedEvent @event)
    {
        return new GameCellOpenedEventDto(@event.GameId, @event.Version, ToDto(@event.Cell));
    }

    private static GameBoardCellDto ToDto(GameBoardCell cell)
    {
        return new GameBoardCellDto(
            cell.Id,
            cell.Row,
            cell.Col,
            cell.CellType,
            cell.Title,
            cell.Description,
            cell.Cost,
            cell.State.ToString().ToLowerInvariant(),
            cell.Media.Select(media => new GameBoardCellMediaDto(media.Url)).ToArray()
        );
    }

    private static AuthRole? TryMapAuthRole(string role)
    {
        return role switch
        {
            AuthRoleCodes.Admin => AuthRole.Admin,
            AuthRoleCodes.Moderator => AuthRole.Moderator,
            AuthRoleCodes.Viewer => AuthRole.Viewer,
            _ => null
        };
    }
}
