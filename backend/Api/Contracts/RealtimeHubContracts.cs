namespace backend.Api.Contracts;
public static class RealtimeHubContracts
{
    public static class GameBoard
    {
        public const string HubPath = "/hubs/game-board";
        public const string CellOpenedEvent = "cellOpened";
        public const string RoundStateChangedEvent = "roundStateChanged";
        public const string ModifierActivatedEvent = "modifierActivated";
        public const string ModifierActivationCancelledEvent = "modifierActivationCancelled";
        public const string QuizStateChangedEvent = "quizStateChanged";
        public const string UserNotificationCreatedEvent = "userNotificationCreated";
    }

    public static class GameSetup
    {
        public const string HubPath = "/hubs/game-setup";
        public const string DraftChangedEvent = "draftChanged";
    }
}
