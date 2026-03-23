using backend.Api.Contracts;
using backend.Application.Contracts;

namespace backend.Application.Mapping;

public static class ApiContractMapper
{
    public static LeaderboardSummaryDto ToDto(this LeaderboardSummary summary)
    {
        return new LeaderboardSummaryDto(
            summary.UpdatedAt,
            summary.Teams
                .Select(team => new LeaderboardTeamDto(
                    team.Id,
                    team.Name,
                    team.ColorHex,
                    team.Score,
                    team.Penalty
                ))
                .ToArray()
        );
    }

    public static LoadoutBoardDto ToDto(this LoadoutBoard board)
    {
        return new LoadoutBoardDto(
            board.Rows,
            board.Cols,
            board.RowLabels.ToArray(),
            board.ColLabels.ToArray(),
            board.Cells
                .Select(cell => new LoadoutCellDto(
                    cell.Id,
                    cell.Row,
                    cell.Col,
                    cell.Label,
                    cell.Points,
                    cell.ImageUrl,
                    cell.State.ToString().ToLowerInvariant()
                ))
                .ToArray()
        );
    }

    public static ModifiersSnapshotDto ToDto(this ModifiersSnapshot snapshot)
    {
        return new ModifiersSnapshotDto(
            snapshot.Available
                .Select(definition => new ModifierDefinitionDto(
                    definition.Id,
                    definition.Name,
                    definition.Cost,
                    definition.Description
                ))
                .ToArray(),
            snapshot.Active
                .Select(modifier => new ActiveModifierDto(
                    modifier.Id,
                    modifier.ModifierId,
                    modifier.ActivatedAt,
                    modifier.TriggeredBy
                ))
                .ToArray()
        );
    }

    public static ActivateModifierCommand ToCommand(this ActivateModifierRequest request)
    {
        return new ActivateModifierCommand(request.ModifierId, request.TriggeredBy);
    }

    public static GameControlStateDto ToDto(this GameControlState state)
    {
        return new GameControlStateDto(
            state.Phase.ToString().ToLowerInvariant(),
            state.CurrentRound,
            state.TotalRounds,
            state.LastActionAt
        );
    }
}
