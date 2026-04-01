using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;

namespace backend.Api.Mapping;

public static class ApiContractMapper
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

    public static GameBoardSnapshotDto ToDto(this GameBoardSnapshot snapshot)
    {
        return new GameBoardSnapshotDto(
            snapshot.GameId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.Rows,
            snapshot.Cols,
            snapshot.RowLabels.ToArray(),
            snapshot.ColLabels.ToArray(),
            snapshot.Cells
                .Select(cell => new GameBoardCellDto(
                    cell.Id,
                    cell.Row,
                    cell.Col,
                    cell.CellType,
                    cell.Title,
                    cell.Description,
                    cell.Cost,
                    cell.State.ToString().ToLowerInvariant(),
                    cell.Media.Select(media => new GameBoardCellMediaDto(media.Url)).ToArray()
                ))
                .ToArray()
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
