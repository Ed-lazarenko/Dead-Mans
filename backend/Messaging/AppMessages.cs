namespace backend.Messaging;
public static class AppMessages
{
    public static class Client
    {
        public const string AuthenticationRequired = "Authentication is required.";
        public const string AccessDenied = "You do not have access to this resource.";
        public const string AuthCookieMissingClaims = "Auth cookie is missing required user claims.";
        public const string UserMissingOrInactive = "User no longer exists or is inactive.";
        public const string LogoutRequiresApiClientHeader =
            "Logout must be initiated from the application.";
        public const string NoActiveOrFinishedGame = "No active or finished game was found.";
        public const string UnableToLoadCurrentGame = "Unable to load the current game.";
        public const string GameCellNotFound = "Requested game cell was not found.";
        public const string UnableToOpenGameCell = "Unable to open the requested game cell.";
        public const string GameActiveTeamRequired =
            "Select the active team before opening cards.";
        public const string GameActiveTeamNoActiveGame =
            "No active game is available for selecting the active team.";
        public const string GameActiveTeamNotFound =
            "Requested team was not found for the active game.";
        public const string GameActiveTeamNotConfirmed =
            "Only confirmed teams can become the active team.";
        public const string GameActiveTeamHasNoActiveMembers =
            "Active team must have at least one active member.";
        public const string NoDraftGameForSetup = "No draft game is available for setup.";
        public const string DraftGameAlreadyExists = "A draft game is already being configured.";
        public const string InvalidGameSetupTitle = "Game title must be between 1 and 200 characters.";
        public const string UnableToLoadGameSetup = "Unable to load the game setup.";
        public const string UnableToCreateGameSetup = "Unable to create the game setup.";
        public const string UnableToSaveGameSetup = "Unable to save the game setup.";
        public const string UnableToDeleteGameSetup = "Unable to delete the game setup.";
        public const string InvalidGameSetupSaveRequest =
            "Game setup could not be saved. Check the title, rows, columns, and card fields.";
        public const string GameSetupCellNotFound = "Requested game setup cell was not found.";
        public const string GameSetupCellMediaNotFound = "Requested game setup cell media was not found.";
        public const string InvalidGameSetupCellMediaUpload =
            "Cell image must be a supported image file up to 5 MB.";
        public const string UnableToUploadGameSetupCellMedia = "Unable to upload the game setup cell image.";
        public const string UnableToDeleteGameSetupCellMedia = "Unable to delete the game setup cell image.";
        public const string GameSetupDraftVersionConflict =
            "The draft was changed by another session. Reload the latest setup and try again.";
        public const string ReadyGameAlreadyExists = "A game is already open for registration.";
        public const string ActiveGameAlreadyExists = "An active game is already in progress.";
        public const string GameNotReadyForStart = "No game is ready to start.";
        public const string GameNotActiveForFinish = "No active game is available to finish.";
        public const string GameRegistrationSlotsRequired =
            "Configure at least one team slot before opening registration.";
        public const string DraftGameDeleteNotAllowed =
            "Draft game can only be removed through game setup draft deletion.";
        public const string GameLifecycleGameNotFound = "Requested game was not found.";
        public const string GameRegistrationNotOpen = "Registration is not open for a ready game.";
        public const string GameRegistrationNoSlots = "No team slots are available.";
        public const string GameRegistrationAlreadyOnTeam = "You are already on a team for this game.";
        public const string GameRegistrationTeamNotFound = "Team was not found.";
        public const string GameRegistrationTeamNotJoinable =
            "This team cannot be joined or confirmed in its current state.";
        public const string GameRegistrationNotTeamMember = "You are not on a team for this game.";
        public const string GameRegistrationInvitationInvalid =
            "Invitation was not found or is no longer pending.";
        public const string GameRegistrationSlotNotFound = "Participation slot was not found.";
        public const string GameRegistrationSlotNotAvailable = "Participation slot is not available.";
        public const string GameRegistrationInvalidTeamSizeLimits =
            "Minimum players per team cannot exceed the maximum.";
        public const string GameLifecycleNoConfirmedTeams =
            "Confirm at least one team before starting the game.";
        public const string GameLifecycleUnconfirmedTeams =
            "Resolve all forming teams before starting the game.";
        public const string GameLifecyclePendingInvitations =
            "Resolve all pending team invitations before starting the game.";
        public const string GameLifecyclePendingDisbandRequests =
            "Resolve all team disband requests before starting the game.";
        public const string GameLifecycleInvalidConfirmedTeamRoster =
            "Confirmed teams must match the configured player limits before starting the game.";
        public const string GameRegistrationPendingInvitationExists =
            "This player already has a pending invitation for this game.";
        public const string GameRegistrationPendingOutgoingInvitation =
            "Cancel the pending invitation before leaving the team.";
        public const string GameRegistrationTeamInviteNotAllowed =
            "You cannot invite players from this team in its current state.";
        public const string GameRegistrationTeamActiveInGame =
            "The active team cannot be disbanded while it is taking its turn.";
        public const string GameRegistrationOperationFailed = "The registration operation could not be completed.";
        public const string NoCurrentGameBoard = "No current game board was found.";
        public const string GameModifierGameNotActive = "No active game is available for modifier activation.";
        public const string GameModifierNotEnabled =
            "Requested game modifier is not enabled for the current game.";
        public const string GameModifierConflictActive =
            "Requested game modifier conflicts with another active modifier.";
        public const string GameModifierLimitReached =
            "Requested game modifier reached its activation limit for the current game.";
        public const string GameModifierInvalidRequest = "Modifier request payload is invalid.";
        public const string GameModifierNotFound = "Requested modifier was not found.";
        public const string GameCardRunNoActiveGame = "No active game is available for starting a card run.";
        public const string GameCardRunCellNotFound = "Requested game cell was not found for the active game.";
        public const string GameCardRunCellNotOpen = "Game cell must be open before starting a card run.";
        public const string GameCardRunTeamNotFound = "Requested team was not found for the active game.";
        public const string GameCardRunTeamNotConfirmed =
            "Only confirmed teams can start a card run.";
        public const string GameCardRunTeamHasNoActiveMembers =
            "Team must have at least one active member to start a card run.";
        public const string GameCardRunAlreadyInProgress =
            "Another card run is already in progress for the active game.";
        public const string GameCardRunInvalidRequest = "Card run request payload is invalid.";
        public const string GameCardRunNotFound = "Requested card run was not found.";
        public const string GameCardRunNotInProgress =
            "Card run cannot be finalized because it is already closed.";
        public const string GameCardRunModifierResultNotFound =
            "Requested modifier resolution was not found for this card run.";
        public const string GameQuestionInvalidRequest = "Question request payload is invalid.";
        public const string GameQuestionDuplicateCode =
            "A question with this code already exists.";
        public const string GameQuestionNotFound = "Requested question was not found.";
        public const string GameQuestionCategoryNotFound = "Requested question category was not found.";
        public const string GameQuestionCategoryNotEmpty =
            "Question category cannot be deleted while it still contains questions.";
        public const string GameQuestionCategoryProtected =
            "System fallback question category cannot be renamed or deleted.";
        public const string GameQuestionNoActiveGame = "No active game is available for asking questions.";
        public const string GameQuestionNoAvailableQuestions =
            "No enabled questions are available for this game.";
        public const string GameQuestionManualAwardPlayerNotFound =
            "Selected player was not found or is inactive.";
        public const string GameQuestionManualAwardInvalidPoints =
            "Manual quiz award points must be greater than zero.";
        public const string GameQuestionRoundNotFound = "Question round was not found.";
        public const string GameQuestionRoundNotPending =
            "Question round cannot be answered because it is already closed.";
        public const string UnexpectedServerError = "An unexpected server error occurred.";
        public const string TooManyRequests = "Too many requests. Please slow down and try again.";
    }

    public static class ErrorCodes
    {
        public const string GameBoardNotFound = "game_board.not_found";
        public const string GameBoardCellNotFound = "game_board.cell_not_found";
        public const string GameBoardActiveTeamRequired = "game_board.active_team_required";
        public const string GameBoardActiveTeamNoActiveGame = "game_board.active_team_no_active_game";
        public const string GameBoardActiveTeamNotFound = "game_board.active_team_not_found";
        public const string GameBoardActiveTeamNotConfirmed = "game_board.active_team_not_confirmed";
        public const string GameBoardActiveTeamHasNoActiveMembers =
            "game_board.active_team_has_no_active_members";
        public const string GameSetupNoDraft = "game_setup.no_draft";
        public const string GameSetupDraftExists = "game_setup.draft_exists";
        public const string InvalidGameSetupTitle = "game_setup.invalid_title";
        public const string GameSetupInvalidSaveRequest = "game_setup.invalid_save_request";
        public const string GameSetupCellNotFound = "game_setup.cell_not_found";
        public const string GameSetupCellMediaNotFound = "game_setup.cell_media_not_found";
        public const string GameSetupInvalidCellMediaUpload = "game_setup.invalid_cell_media_upload";
        public const string GameSetupDraftVersionConflict = "game_setup.stale_version";
        public const string GameLifecycleDraftNotFound = "game_lifecycle.draft_not_found";
        public const string GameLifecycleReadyAlreadyExists = "game_lifecycle.ready_already_exists";
        public const string GameLifecycleActiveAlreadyExists = "game_lifecycle.active_already_exists";
        public const string GameLifecycleGameNotReady = "game_lifecycle.game_not_ready";
        public const string GameLifecycleGameNotActive = "game_lifecycle.game_not_active";
        public const string GameLifecycleRegistrationSlotsRequired =
            "game_lifecycle.registration_slots_required";
        public const string GameLifecycleInvalidTeamSizeLimits =
            "game_lifecycle.invalid_team_size_limits";
        public const string GameLifecycleNoConfirmedTeams =
            "game_lifecycle.no_confirmed_teams";
        public const string GameLifecycleUnconfirmedTeams =
            "game_lifecycle.unconfirmed_teams";
        public const string GameLifecyclePendingInvitations =
            "game_lifecycle.pending_invitations";
        public const string GameLifecyclePendingDisbandRequests =
            "game_lifecycle.pending_disband_requests";
        public const string GameLifecycleInvalidConfirmedTeamRoster =
            "game_lifecycle.invalid_confirmed_team_roster";
        public const string GameLifecycleOperationFailed = "game_lifecycle.operation_failed";
        public const string GameLifecycleDraftDeleteNotAllowed =
            "game_lifecycle.draft_delete_not_allowed";
        public const string GameLifecycleGameNotFound = "game_lifecycle.game_not_found";
        public const string UnexpectedServerError = "game_common.unexpected_server_error";
        public const string TooManyRequests = "game_common.too_many_requests";
        public const string GameRegistrationNotOpen = "game_registration.not_open";
        public const string GameRegistrationNoSlots = "game_registration.no_slots";
        public const string GameRegistrationAlreadyOnTeam = "game_registration.already_on_team";
        public const string GameRegistrationTeamNotFound = "game_registration.team_not_found";
        public const string GameRegistrationTeamNotJoinable = "game_registration.team_not_joinable";
        public const string GameRegistrationNotTeamMember = "game_registration.not_team_member";
        public const string GameRegistrationInvitationInvalid = "game_registration.invitation_invalid";
        public const string GameRegistrationSlotNotFound = "game_registration.slot_not_found";
        public const string GameRegistrationSlotNotAvailable = "game_registration.slot_not_available";
        public const string GameRegistrationUserNotFound = "game_registration.user_not_found";
        public const string GameRegistrationPendingInvitation = "game_registration.pending_invitation";
        public const string GameRegistrationPendingOutgoingInvitation =
            "game_registration.pending_outgoing_invitation";
        public const string GameRegistrationTeamInviteNotAllowed =
            "game_registration.team_invite_not_allowed";
        public const string GameRegistrationTeamActiveInGame =
            "game_registration.team_active_in_game";
        public const string GameRegistrationOperationFailed = "game_registration.operation_failed";
        public const string GameModifierGameNotActive = "game_modifier.game_not_active";
        public const string GameModifierNotEnabled = "game_modifier.not_enabled";
        public const string GameModifierConflictActive = "game_modifier.conflict_active";
        public const string GameModifierLimitReached = "game_modifier.limit_reached";
        public const string GameModifierUserNotResolved = "game_modifier.user_not_resolved";
        public const string GameModifierInvalidRequest = "game_modifier.invalid_request";
        public const string GameModifierNotFound = "game_modifier.not_found";
        public const string GameCardRunNoActiveGame = "game_card_run.no_active_game";
        public const string GameCardRunCellNotFound = "game_card_run.cell_not_found";
        public const string GameCardRunCellNotOpen = "game_card_run.cell_not_open";
        public const string GameCardRunTeamNotFound = "game_card_run.team_not_found";
        public const string GameCardRunTeamNotConfirmed = "game_card_run.team_not_confirmed";
        public const string GameCardRunTeamHasNoActiveMembers =
            "game_card_run.team_has_no_active_members";
        public const string GameCardRunAlreadyInProgress = "game_card_run.already_in_progress";
        public const string GameCardRunInvalidRequest = "game_card_run.invalid_request";
        public const string GameCardRunNotFound = "game_card_run.not_found";
        public const string GameCardRunNotInProgress = "game_card_run.not_in_progress";
        public const string GameCardRunModifierResultNotFound =
            "game_card_run.modifier_result_not_found";
        public const string GameQuestionInvalidRequest = "game_question.invalid_request";
        public const string GameQuestionDuplicateCode = "game_question.duplicate_code";
        public const string GameQuestionNotFound = "game_question.not_found";
        public const string GameQuestionCategoryNotFound = "game_question.category_not_found";
        public const string GameQuestionCategoryNotEmpty = "game_question.category_not_empty";
        public const string GameQuestionCategoryProtected = "game_question.category_protected";
        public const string GameQuestionNoActiveGame = "game_question.no_active_game";
        public const string GameQuestionNoAvailableQuestions = "game_question.no_available_questions";
        public const string GameQuestionManualAwardPlayerNotFound =
            "game_question.manual_award_player_not_found";
        public const string GameQuestionManualAwardInvalidPoints =
            "game_question.manual_award_invalid_points";
        public const string GameQuestionRoundNotFound = "game_question.round_not_found";
        public const string GameQuestionRoundNotPending = "game_question.round_not_pending";
        public const string GameQuestionImportInvalidFields = "game_question.import_invalid_fields";
        public const string GameQuestionImportDuplicateCodeInFile =
            "game_question.import_duplicate_code_in_file";
        public const string GameQuestionImportCategoryUnresolved =
            "game_question.import_category_unresolved";
        public const string GameQuestionImportDuplicateCodeExisting =
            "game_question.import_duplicate_code_existing";
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
        public const string GameCellNotFoundForOpen = "Cannot open game cell because it was not found. CellId: {CellId}.";
        public const string GameCellAlreadyOpen = "Game cell is already open. CellId: {CellId}.";
        public const string GameCellOpened = "Game cell opened. CellId: {CellId}.";
        public const string GameCellOpenFailed = "Failed to open game cell. CellId: {CellId}.";
        public const string RealtimeGameCellOpenedPublishFailed =
            "Failed to publish game cell opened realtime event. CellId: {CellId}.";
        public const string RealtimeGameModifierActivatedPublishFailed =
            "Failed to publish game modifier activated realtime event. ModifierId: {ModifierId}.";
        public const string RealtimeGameSetupDraftChangedPublishFailed =
            "Failed to publish game setup draft changed realtime event.";

        public const string AuthSessionMissingClaim =
            "Auth session request missing or invalid NameIdentifier claim.";

        public const string AuthSessionUserGone =
            "Auth session not found or user inactive; signing out. UserId: {UserId}.";

        public const string UserSignedOut = "User signed out.";

        public const string GameSetupDraftNotFound = "No draft game with a board was found for setup.";
        public const string GameSetupDraftLoadFailed = "Failed to load draft game setup.";
        public const string GameSetupDraftCreateFailed = "Failed to create draft game setup.";
        public const string GameSetupDraftAlreadyExists = "Draft game setup already exists.";
        public const string GameSetupDraftCreated =
            "Draft game setup created. GameId: {GameId}, CellCount: {CellCount}.";
        public const string GameSetupDraftSaved =
            "Draft game setup saved. GameId: {GameId}, BoardVersion: {BoardVersion}.";
        public const string GameSetupDraftVersionConflict =
            "Draft game setup save rejected due to version conflict. GameId: {GameId}, ExpectedVersion: {ExpectedVersion}, CurrentVersion: {CurrentVersion}.";
        public const string GameSetupDraftSaveFailed = "Failed to save draft game setup.";
        public const string GameSetupDraftDeleted = "Draft game setup deleted. GameId: {GameId}.";
        public const string GameSetupDraftDeleteFailed = "Failed to delete draft game setup.";
        public const string GameSetupDraftMediaStorageCleanupFailed =
            "Failed to clean up draft game setup media in object storage. GameId: {GameId}, Prefix: {Prefix}.";
        public const string GameSetupCellMediaUploadFailed =
            "Failed to upload draft game setup cell media. CellId: {CellId}.";
        public const string GameSetupCellMediaStorageUploadFailed =
            "Failed to upload draft game setup cell media to storage. CellId: {CellId}, GameId: {GameId}.";
        public const string GameSetupCellMediaDeleteFailed =
            "Failed to delete draft game setup cell media. CellId: {CellId}.";
        public const string GameSetupCellMediaObjectCleanupFailed =
            "Failed to clean up draft game setup cell media object. CellId: {CellId}, Bucket: {Bucket}, ObjectKey: {ObjectKey}.";
    }
}
