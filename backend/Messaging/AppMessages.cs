namespace backend.Messaging;

/// <summary>
/// API error payloads, exception messages, and Microsoft.Extensions.Logging message templates in one place.
/// </summary>
public static class AppMessages
{
    public static class Client
    {
        public const string AuthenticationRequired = "Authentication is required.";
        public const string AccessDenied = "You do not have access to this resource.";
        public const string AuthCookieMissingClaims = "Auth cookie is missing required user claims.";
        public const string UserMissingOrInactive = "User no longer exists or is inactive.";
        public const string UnableToLoadLoadout = "Unable to load loadout board.";
        public const string UnableToUpdateLoadoutCell = "Unable to update loadout cell.";
        public const string NoActiveOrFinishedGame = "No active or finished game was found.";
        public const string UnableToLoadCurrentGame = "Unable to load the current game.";
    }

    public static class Exceptions
    {
        public const string AuthRequiresApplicationDbContext =
            "Authentication requires a configured ApplicationDbContext. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.";

        public const string AuthRequiresEfProvider =
            "Authentication requires a configured EF Core provider. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.";

        public const string NoLoadoutBoardInDatabase = "No loadout board exists in the database.";

        public static string LoadoutCellIdNotValidGuid(string cellId) =>
            $"Loadout cell id '{cellId}' is not a valid GUID.";

        public static string LoadoutCellNotFound(string cellId) =>
            $"Loadout cell '{cellId}' was not found.";

        public static string LoadoutBoardMetadataNotFound(Guid boardId) =>
            $"Loadout board '{boardId}' metadata was not found.";

        public static string LoadoutBoardHasNoCells(Guid boardId) =>
            $"Loadout board '{boardId}' does not contain any cells.";

        public const string UnavailableLeaderboard =
            "Leaderboard repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint.";

        public const string UnavailableModifiers =
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint.";

        public const string UnavailableGameControl =
            "Game control repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint.";

        public const string ViewerRoleMissing = "Viewer role was not found in roles table.";

        public static string TwitchTokenExchangeFailed(int statusCode, string error) =>
            $"Twitch token exchange failed with status {statusCode}: {error}";

        public const string TwitchTokenResponseEmpty = "Twitch token response is empty.";

        public static string TwitchUserRequestFailed(int statusCode, string error) =>
            $"Twitch user request failed with status {statusCode}: {error}";

        public const string TwitchUsersResponseEmpty = "Twitch users response is empty.";

        public const string TwitchUsersResponseNoUser = "Twitch users response contains no user.";
    }

    public static class Logs
    {
        public const string ApplicationTerminatedUnexpectedly = "Application terminated unexpectedly.";

        public const string ApplicationDbContextNotRegistered =
            "ApplicationDbContext is not registered. Auth requires a configured database.";

        public const string EfProviderNameEmpty = "EF Core provider name is empty; database is not configured.";

        public const string FailedToOpenDatabaseOnStartup =
            "Failed to open database connection during startup validation.";

        public const string AuthPersistenceValidated =
            "Auth persistence validated: database provider is {ProviderName}.";

        public const string UnavailableLeaderboard =
            "Leaderboard persistence is not configured; rejecting request. Implement ILeaderboardRepository.";

        public const string UnavailableModifiersRequest =
            "Modifiers persistence is not configured; rejecting request. Implement IModifiersRepository.";

        public const string UnavailableModifiersSave =
            "Modifiers persistence is not configured; rejecting save. Implement IModifiersRepository.";

        public const string UnavailableGameControlRequest =
            "Game control persistence is not configured; rejecting request. Implement IGameControlRepository.";

        public const string UnavailableGameControlSave =
            "Game control persistence is not configured; rejecting save. Implement IGameControlRepository.";

        public const string TwitchOAuthErrorQuery =
            "Twitch OAuth returned error query parameter: {OAuthError}.";

        public const string TwitchOAuthMissingCode = "Twitch OAuth callback missing authorization code.";
        public const string TwitchOAuthMissingState = "Twitch OAuth callback missing state parameter.";
        public const string TwitchOAuthStateCookieMissing = "Twitch OAuth state cookie is missing.";
        public const string TwitchOAuthStateMismatch = "Twitch OAuth state did not match state cookie.";

        public const string TwitchUserSignedIn =
            "User signed in via Twitch. UserId: {UserId}, IsNewUser: {IsNewUser}.";

        public const string TwitchInactiveUserSignIn =
            "Inactive user attempted Twitch sign-in. UserId: {UserId}.";

        public const string TwitchAuthCallbackFailed =
            "Twitch authentication callback failed before redirect.";

        public const string TwitchAuthTokenExchangeFailed =
            "Twitch authentication failed during token exchange or persistence.";

        public const string TwitchTokenExchangeHttpFailed =
            "Twitch token exchange failed with status {StatusCode}.";

        public const string TwitchHelixUsersRequestFailed =
            "Twitch Helix users request failed with status {StatusCode}.";

        public const string TwitchHelixNoUserEntries =
            "Twitch Helix users response contained no user entries.";

        public const string DbLoadoutBoardLoadError = "Database error while loading loadout board.";

        public const string DbLoadoutToggleCellError =
            "Database error while toggling loadout cell. CellId: {CellId}.";

        public const string DbGameBoardLoadError = "Database error while loading current game board.";

        public const string GameHasNoBoardRow = "Game {GameId} has no board row; cannot build snapshot.";

        public const string NoActiveOrFinishedGameRow = "No active or finished game row found.";

        public const string GameBoardSnapshotResolved =
            "Resolved game board snapshot. GameId: {GameId}, Status: {Status}, CellCount: {CellCount}.";

        public const string DbAuthUserResolveError = "Database error while resolving auth user {UserId}.";

        public const string ViewerRoleMissingFromTable =
            "Viewer role '{ViewerRoleCode}' is missing from the roles table.";

        public const string RoleClaimsSkipHydrationMissingGuid =
            "Role claims: skip hydration, NameIdentifier missing or not a GUID.";

        public const string RoleClaimsSkipHydrationInactiveUser =
            "Role claims: skip hydration for user {UserId} (missing or inactive).";

        public const string LeaderboardLoadFailed =
            "Leaderboard load failed (configuration or domain rule).";

        public const string LeaderboardUnexpectedError = "Unexpected error loading leaderboard.";

        public const string ModifiersSnapshotFailed =
            "Modifiers snapshot failed (configuration or domain rule).";

        public const string ModifiersUnexpectedLoadError = "Unexpected error loading modifiers.";

        public const string ModifierActivateRequested =
            "Modifier activate requested. ModifierId: {ModifierId}.";

        public const string ModifierActivateFailed = "Modifier activate failed.";

        public const string ModifierActivateUnexpectedError = "Unexpected error activating modifier.";

        public const string GameStateReadFailed =
            "Game state read failed (configuration or domain rule).";

        public const string GameStateUnexpectedLoadError = "Unexpected error loading game state.";

        public const string GameControlStartRequested = "Game control: start requested.";
        public const string GameStateStartFailed = "Game state start failed.";
        public const string GameStartUnexpectedError = "Unexpected error starting game.";

        public const string GameControlPauseRequested = "Game control: pause requested.";
        public const string GameStatePauseFailed = "Game state pause failed.";
        public const string GamePauseUnexpectedError = "Unexpected error pausing game.";

        public const string GameControlResumeRequested = "Game control: resume requested.";
        public const string GameStateResumeFailed = "Game state resume failed.";
        public const string GameResumeUnexpectedError = "Unexpected error resuming game.";

        public const string GameControlNextRoundRequested = "Game control: next round requested.";
        public const string GameStateNextRoundFailed = "Game state next-round failed.";
        public const string GameNextRoundUnexpectedError = "Unexpected error advancing round.";

        public const string GameControlResetRequested = "Game control: reset requested.";
        public const string GameStateResetFailed = "Game state reset failed.";
        public const string GameResetUnexpectedError = "Unexpected error resetting game.";

        public const string LoadoutBoardLoadFailed = "Failed to load loadout board.";

        public const string LoadoutInvalidToggle =
            "Invalid loadout cell toggle request. CellId: {CellId}.";

        public const string LoadoutToggleUnexpected =
            "Unexpected error toggling loadout cell. CellId: {CellId}.";

        public const string GameNoBoardForGet = "No active or finished game with a board was found.";
        public const string GameBoardLoadFailed = "Failed to load current game board.";

        public const string AuthSessionMissingClaim =
            "Auth session request missing or invalid NameIdentifier claim.";

        public const string AuthSessionUserGone =
            "Auth session not found or user inactive; signing out. UserId: {UserId}.";

        public const string UserSignedOut = "User signed out.";
    }
}
