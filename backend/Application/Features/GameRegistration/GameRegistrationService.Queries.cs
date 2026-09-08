using backend.Application.Contracts;

namespace backend.Application.Features.GameRegistration;

public sealed partial class GameRegistrationService
{
    public async Task<GameRegistrationSnapshot?> GetRegistrationSnapshotAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.BuildSnapshotAsync(game.GameId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<RegistrationTeamDto>?> ListTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.LoadTeamsForGameAsync(game.GameId, cancellationToken);
    }

    public async Task<GameRegistrationAdminSnapshot?> GetAdminSnapshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.BuildAdminSnapshotAsync(game.GameId, cancellationToken);
    }
}
