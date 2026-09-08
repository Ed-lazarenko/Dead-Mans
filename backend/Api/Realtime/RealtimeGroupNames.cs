namespace backend.Api.Realtime;

internal static class RealtimeGroupNames
{
    public const string GameBoardAudience = "game-board";
    public const string GameSetupAudience = "game-setup";

    public static string GameBoardUserAudience(Guid userId)
    {
        return $"game-board-user:{userId:N}";
    }
}
