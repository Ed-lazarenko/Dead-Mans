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
        public const string NoActiveOrFinishedGame = "No active or finished game was found.";
        public const string UnableToLoadCurrentGame = "Unable to load the current game.";
    }

    public static class Exceptions
    {
        public const string AuthRequiresApplicationDbContext =
            "Authentication requires a configured ApplicationDbContext. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.";

        public const string AuthRequiresEfProvider =
            "Authentication requires a configured EF Core provider. Set ConnectionStrings:DefaultConnection for the backend or override ApplicationDbContext explicitly for tests.";

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

        public const string GameNoBoardForGet = "No active or finished game with a board was found.";
        public const string GameBoardLoadFailed = "Failed to load current game board.";

        public const string AuthSessionMissingClaim =
            "Auth session request missing or invalid NameIdentifier claim.";

        public const string AuthSessionUserGone =
            "Auth session not found or user inactive; signing out. UserId: {UserId}.";

        public const string UserSignedOut = "User signed out.";
    }
}
