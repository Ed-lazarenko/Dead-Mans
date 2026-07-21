namespace backend.Api.Contracts;
public static class RealtimeHubContracts
{
    public static class GameBoard
    {
        public const string HubPath = "/hubs/game-board";
        public const string CellOpenedEvent = "cellOpened";
        public const string ModifierActivatedEvent = "modifierActivated";
        public const string ModifierActivationCancelledEvent = "modifierActivationCancelled";
    }

    public static class GameSetup
    {
        public const string HubPath = "/hubs/game-setup";
        public const string DraftChangedEvent = "draftChanged";
    }
}
