using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameLifecycleContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameLifecycleContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenRegistration_WhenAnonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenRegistration_WhenNoDraft_ReturnsNotFound()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.NoDraftGameForSetup, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleDraftNotFound, payload.Code);
    }

    [Fact]
    public async Task OpenRegistration_WhenDraftExists_MovesGameToReady()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft for registration")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Ready, payload.Status);
    }

    [Fact]
    public async Task Start_WhenReadyGameExists_MovesGameToActive()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Ready to start")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var openResponse = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameLifecycleStateDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Active, payload.Status);
    }

    [Fact]
    public async Task Finish_WhenActiveGameExists_MovesGameToFinished()
    {
        await ClearGamesAsync();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(adminId, "finish-admin");
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Active round"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        var board = await adminClient.GetFromJsonAsync<GameBoardSnapshotDto>("/api/game");
        Assert.NotNull(board);
        var previewResponse = await adminClient.GetAsync(
            $"/api/game/lifecycle/games/{board!.GameId}/finish-preview"
        );
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<GameFinishPreviewDto>();
        Assert.NotNull(preview);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/lifecycle/games/{board.GameId}/finish",
            new FinishGameRequestDto(
                board.Version,
                Guid.NewGuid(),
                preview!.Warnings.Select(x => x.Code).ToArray(),
                "Official result"
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FinishGameResponseDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameStatusValue.Finished, payload.Summary.GameStatus);
        Assert.Equal("Official result", payload.Summary.PublicNote);
        Assert.Equal(board.Version + 1, payload.Summary.BoardVersion);
        Assert.Single(payload.Summary.Teams);
        Assert.Null(payload.Summary.Teams[0].Placement);

        var history = await adminClient.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{board.GameId}"
        );
        Assert.NotNull(history?.FinalResult);
        Assert.Equal("Official result", history.FinalResult.PublicNote);
        Assert.Null(Assert.Single(history.FinalResult.Teams).FinalScore);
    }

    [Theory]
    [InlineData(AuthRoleCodes.Moderator)]
    [InlineData(AuthRoleCodes.Viewer)]
    public async Task FinishEndpoints_WhenCallerIsNotAdmin_ReturnForbidden(string role)
    {
        using var client = TestAuthClientFactory.CreateClient(_factory, [role]);
        var gameId = Guid.NewGuid();

        var preview = await client.GetAsync(
            $"/api/game/lifecycle/games/{gameId}/finish-preview"
        );
        var finish = await client.PostAsJsonAsync(
            $"/api/game/lifecycle/games/{gameId}/finish",
            new FinishGameRequestDto(1, Guid.NewGuid(), [], null)
        );

        Assert.Equal(HttpStatusCode.Forbidden, preview.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, finish.StatusCode);
    }

    [Fact]
    public async Task Finish_WhenWarningsAreNotAcknowledged_ReturnsConflict()
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync("warnings");
        using (adminClient)
        {
            var response = await adminClient.PostAsJsonAsync(
                $"/api/game/lifecycle/games/{board.GameId}/finish",
                new FinishGameRequestDto(board.Version, Guid.NewGuid(), [], null)
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(AppMessages.ErrorCodes.GameFinishWarningsNotAcknowledged, error?.Code);
        }
    }

    [Fact]
    public async Task Finish_WhenBoardVersionIsStale_ReturnsConflict()
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync("stale-version");
        using (adminClient)
        {
            var response = await adminClient.PostAsJsonAsync(
                $"/api/game/lifecycle/games/{board.GameId}/finish",
                new FinishGameRequestDto(
                    board.Version + 1,
                    Guid.NewGuid(),
                    [
                        GameFinishWarningCodes.UnplayedTeams,
                        GameFinishWarningCodes.NoCompletedRounds
                    ],
                    null
                )
            );

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(AppMessages.ErrorCodes.GameFinishStaleVersion, error?.Code);
        }
    }

    [Fact]
    public async Task Finish_WhenRepeated_ReturnsSameImmutableSnapshot()
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync("idempotent");
        using (adminClient)
        {
            var requestId = Guid.NewGuid();
            var warnings = new[]
            {
                GameFinishWarningCodes.UnplayedTeams,
                GameFinishWarningCodes.NoCompletedRounds
            };
            var first = await adminClient.PostAsJsonAsync(
                $"/api/game/lifecycle/games/{board.GameId}/finish",
                new FinishGameRequestDto(board.Version, requestId, warnings, "original")
            );
            var second = await adminClient.PostAsJsonAsync(
                $"/api/game/lifecycle/games/{board.GameId}/finish",
                new FinishGameRequestDto(board.Version, requestId, [], "must not replace")
            );

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var firstPayload = await first.Content.ReadFromJsonAsync<FinishGameResponseDto>();
            var secondPayload = await second.Content.ReadFromJsonAsync<FinishGameResponseDto>();
            Assert.False(firstPayload?.AlreadyFinished);
            Assert.True(secondPayload?.AlreadyFinished);
            Assert.Equal("original", secondPayload?.Summary.PublicNote);
            Assert.Equal(firstPayload?.Summary.BoardVersion, secondPayload?.Summary.BoardVersion);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var gameId = Guid.Parse(board.GameId);
            Assert.Equal(1, await db.GameFinalizations.CountAsync(x => x.GameId == gameId));
        }
    }

    [Theory]
    [InlineData(GameRoundStatusValue.AwaitingModifiers)]
    [InlineData(GameRoundStatusValue.Preparing)]
    [InlineData(GameRoundStatusValue.InProgress)]
    [InlineData(GameRoundStatusValue.ReviewingResults)]
    public async Task FinishPreview_WhenRoundIsNonterminal_ReturnsBlocker(string roundStatus)
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync($"round-{roundStatus}");
        using (adminClient)
        {
            await SeedRoundAsync(Guid.Parse(board.GameId), roundStatus);

            var preview = await adminClient.GetFromJsonAsync<GameFinishPreviewDto>(
                $"/api/game/lifecycle/games/{board.GameId}/finish-preview"
            );

            Assert.NotNull(preview);
            Assert.False(preview.CanFinish);
            Assert.Contains(
                preview.Blockers,
                blocker => blocker.Code == GameFinishBlockerCodes.RoundInProgress
            );
        }
    }

    [Fact]
    public async Task Finish_SkipsAskedQuizAndClearsActiveTeamAtomically()
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync("quiz-cleanup");
        using (adminClient)
        {
            var gameId = Guid.Parse(board.GameId);
            var quizRoundId = await SeedAskedQuizAndActiveTeamAsync(gameId);
            var preview = await adminClient.GetFromJsonAsync<GameFinishPreviewDto>(
                $"/api/game/lifecycle/games/{board.GameId}/finish-preview"
            );
            Assert.Equal(1, preview?.Summary.PendingQuizQuestionCount);

            var response = await adminClient.PostAsJsonAsync(
                $"/api/game/lifecycle/games/{board.GameId}/finish",
                new FinishGameRequestDto(
                    board.Version,
                    Guid.NewGuid(),
                    preview!.Warnings.Select(x => x.Code).ToArray(),
                    null
                )
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await db.Games.SingleAsync(x => x.Id == gameId);
            var quizRound = await db.GameQuizRounds.SingleAsync(x => x.Id == quizRoundId);
            Assert.Null(game.ActiveTeamId);
            Assert.Equal(GameQuizRoundStatusValue.Skipped, quizRound.Status);
            Assert.NotNull(quizRound.ClosedAtUtc);
            Assert.InRange(quizRound.ClosedAtUtc.Value, quizRound.AskedAtUtc, quizRound.ClosesAtUtc);
            Assert.Equal(board.Version + 1, await db.GameBoards
                .Where(x => x.GameId == gameId)
                .Select(x => x.Version)
                .SingleAsync());
        }
    }

    [Fact]
    public async Task FinishPreview_UsesCurrentFormulaAndExcludesCancelledRounds()
    {
        await ClearGamesAsync();
        var (adminClient, board) = await CreateActiveGameAsync("authoritative-score");
        using (adminClient)
        {
            var gameId = Guid.Parse(board.GameId);
            await SeedTerminalRoundAsync(gameId, GameRoundStatusValue.Completed, 115, false, 3);
            await SeedTerminalRoundAsync(gameId, GameRoundStatusValue.Completed, -100, true, 0);
            await SeedTerminalRoundAsync(gameId, GameRoundStatusValue.Cancelled, 2_000, false, 20);

            var preview = await adminClient.GetFromJsonAsync<GameFinishPreviewDto>(
                $"/api/game/lifecycle/games/{board.GameId}/finish-preview"
            );

            Assert.NotNull(preview);
            Assert.True(preview.CanFinish);
            Assert.Equal(2, preview.Summary.CompletedRoundCount);
            Assert.Equal(1, preview.Summary.CancelledRoundCount);
            var team = Assert.Single(preview.Summary.Teams);
            Assert.Equal(115, team.BestScore);
            Assert.Equal(100, team.PenaltyTotal);
            Assert.Equal(15, team.FinalScore);
            Assert.Equal(3, team.TotalKills);
            Assert.Equal(1, team.Placement);
        }
    }

    [Fact]
    public async Task ArchiveGame_WhenFinishedGameExists_SoftDeletesGameAndReturnsNoContent()
    {
        await ClearGamesAsync();
        var adminId = Guid.NewGuid();
        await SeedUserAsync(adminId, "archive-admin");
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Archive me"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await adminClient.PostAsync("/api/game/lifecycle/start", content: null);
        var board = await adminClient.GetFromJsonAsync<GameBoardSnapshotDto>("/api/game");
        Assert.NotNull(board);
        var preview = await adminClient.GetFromJsonAsync<GameFinishPreviewDto>(
            $"/api/game/lifecycle/games/{board!.GameId}/finish-preview"
        );
        Assert.NotNull(preview);
        var finishResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/lifecycle/games/{board.GameId}/finish",
            new FinishGameRequestDto(
                board.Version,
                Guid.NewGuid(),
                preview!.Warnings.Select(x => x.Code).ToArray(),
                null
            )
        );
        Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);
        var finished = await finishResponse.Content.ReadFromJsonAsync<FinishGameResponseDto>();
        Assert.NotNull(finished);

        var archiveResponse = await adminClient.DeleteAsync(
            $"/api/game/lifecycle/games/{finished!.Summary.GameId}"
        );
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var game = await db.Games.FirstAsync(x => x.Id.ToString() == finished.Summary.GameId);
        Assert.True(game.IsDeleted);
        Assert.NotNull(game.DeletedAtUtc);
    }

    [Fact]
    public async Task ArchiveGame_WhenDraftGamePassed_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Draft should hard-delete only")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        var setup = await setupResponse.Content.ReadFromJsonAsync<GameSetupSnapshotDto>();
        Assert.NotNull(setup);

        var archiveResponse = await adminClient.DeleteAsync($"/api/game/lifecycle/games/{setup!.GameId}");
        Assert.Equal(HttpStatusCode.Conflict, archiveResponse.StatusCode);
        var payload = await archiveResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleDraftDeleteNotAllowed, payload.Code);
    }

    [Fact]
    public async Task Start_WhenNoReadyGame_ReturnsNotFound()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameNotReadyForStart, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleGameNotReady, payload.Code);
    }

    [Fact]
    public async Task Start_WhenNoConfirmedTeams_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("No teams"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleNoConfirmedTeams, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleNoConfirmedTeams, payload.Code);
    }

    [Fact]
    public async Task Start_WhenFormingTeamExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Forming team"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1, slotIndex: 1);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Forming, memberCount: 1, slotIndex: 2);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleUnconfirmedTeams, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleUnconfirmedTeams, payload.Code);
    }

    [Fact]
    public async Task Start_WhenPendingInvitationExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Pending invite"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        var teamId = await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        await SeedPendingInvitationForReadyGameAsync(teamId);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecyclePendingInvitations, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecyclePendingInvitations, payload.Code);
    }

    [Fact]
    public async Task Start_WhenDisbandRequestExists_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Disband request"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(
            TeamStatusValue.Confirmed,
            memberCount: 1,
            disbandRequested: true
        );

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecyclePendingDisbandRequests, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecyclePendingDisbandRequests, payload.Code);
    }

    [Fact]
    public async Task Start_WhenConfirmedTeamRosterInvalid_ReturnsConflict()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        await adminClient.PostAsJsonAsync("/api/game/setup", new CreateGameSetupRequestDto("Invalid roster"));
        await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 0);

        var response = await adminClient.PostAsync("/api/game/lifecycle/start", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleInvalidConfirmedTeamRoster, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleInvalidConfirmedTeamRoster, payload.Code);
    }

    [Fact]
    public async Task OpenRegistration_WhenServiceThrows_ReturnsInternalServerErrorPayload()
    {
        using var adminClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            configureServices: services =>
            {
                services.RemoveAll<IGameLifecycleService>();
                services.AddSingleton<IGameLifecycleService>(new ThrowingGameLifecycleService());
            }
        );

        var response = await adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UnexpectedServerError, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.UnexpectedServerError, payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestId));
    }

    [Fact]
    public async Task OpenRegistration_WhenParallelRequests_ProducesOkAndHandledError()
    {
        await ClearGamesAsync();
        using var adminClient = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Admin]);
        var setupResponse = await adminClient.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto("Parallel lifecycle draft")
        );
        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var firstOpen = adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);
        var secondOpen = adminClient.PostAsync("/api/game/lifecycle/open-registration", content: null);

        var responses = await Task.WhenAll(firstOpen, secondOpen);
        var statuses = responses.Select(response => response.StatusCode).ToArray();

        Assert.All(
            statuses,
            status =>
                Assert.Contains(
                    status,
                    new[] { HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.NotFound }
                )
        );
        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
    }

    private async Task<(HttpClient Client, GameBoardSnapshotDto Board)> CreateActiveGameAsync(
        string title
    )
    {
        var adminId = Guid.NewGuid();
        await SeedUserAsync(adminId, $"admin-{Guid.NewGuid():N}");
        var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            adminId
        );
        var setup = await client.PostAsJsonAsync(
            "/api/game/setup",
            new CreateGameSetupRequestDto(title)
        );
        Assert.Equal(HttpStatusCode.Created, setup.StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync("/api/game/lifecycle/open-registration", null)).StatusCode
        );
        await SeedTeamForReadyGameAsync(TeamStatusValue.Confirmed, memberCount: 1);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync("/api/game/lifecycle/start", null)).StatusCode
        );
        var board = await client.GetFromJsonAsync<GameBoardSnapshotDto>("/api/game");
        return (client, Assert.IsType<GameBoardSnapshotDto>(board));
    }

    private async Task SeedRoundAsync(Guid gameId, string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await db.GameTeams.FirstAsync(x => x.GameId == gameId);
        var cell = await db.BoardCells.FirstAsync(x => x.Board.GameId == gameId);
        var now = DateTime.UtcNow;
        db.GameRounds.Add(
            new GameRound
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                BoardId = cell.BoardId,
                BoardCellId = cell.Id,
                TeamId = team.Id,
                Status = status,
                Version = 1,
                BaseScore = cell.Cost,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = cell.RowIndex,
                CellColIndex = cell.ColIndex,
                CellTitleSnapshot = cell.Title,
                CellDescriptionSnapshot = cell.Description,
                CellCostSnapshot = cell.Cost,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedAskedQuizAndActiveTeamAsync(Guid gameId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var teamId = await db.GameTeams
            .Where(x => x.GameId == gameId)
            .Select(x => x.Id)
            .FirstAsync();
        var game = await db.Games.FirstAsync(x => x.Id == gameId);
        game.ActiveTeamId = teamId;
        var now = DateTime.UtcNow;
        var category = new QuestionCategory
        {
            Id = Guid.NewGuid(),
            Name = $"category-{Guid.NewGuid():N}",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var question = new QuestionDefinition
        {
            Id = Guid.NewGuid(),
            ExternalCode = $"question-{Guid.NewGuid():N}",
            CategoryId = category.Id,
            Text = "Question?",
            Reward = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            AcceptedAnswers =
            [
                new QuestionAcceptedAnswer
                {
                    Id = Guid.NewGuid(),
                    AnswerText = "Answer",
                    NormalizedAnswer = "answer",
                    IsPrimary = true,
                    SortOrder = 0,
                    CreatedAtUtc = now
                }
            ]
        };
        var roundId = Guid.NewGuid();
        db.QuestionCategories.Add(category);
        db.QuestionDefinitions.Add(question);
        db.GameQuizRounds.Add(
            new GameQuizRound
            {
                Id = roundId,
                GameId = gameId,
                QuestionId = question.Id,
                AskOrder = 1,
                AskedAtUtc = now,
                ClosesAtUtc = now.AddMinutes(1),
                Status = GameQuizRoundStatusValue.Asked,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = question.ExternalCode,
                CategoryNameSnapshot = category.Name,
                QuestionTextSnapshot = question.Text,
                AcceptedAnswersSnapshot = ["Answer"],
                NormalizedAnswersSnapshot = ["answer"],
                RewardSnapshot = question.Reward,
                DeliveryKind = "manual"
            }
        );
        await db.SaveChangesAsync();
        return roundId;
    }

    private async Task SeedTerminalRoundAsync(
        Guid gameId,
        string status,
        int finalScore,
        bool emptyCardPenaltyApplied,
        int killsCount
    )
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await db.GameTeams.FirstAsync(x => x.GameId == gameId);
        var usedCellIds = await db.GameRounds
            .Where(x => x.GameId == gameId)
            .Select(x => x.BoardCellId)
            .ToArrayAsync();
        var cell = await db.BoardCells.FirstAsync(
            x => x.Board.GameId == gameId && !usedCellIds.Contains(x.Id)
        );
        var now = DateTime.UtcNow;
        db.GameRounds.Add(
            new GameRound
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                BoardId = cell.BoardId,
                BoardCellId = cell.Id,
                TeamId = team.Id,
                Status = status,
                Version = 2,
                FinishedAtUtc = now,
                BaseScore = cell.Cost,
                FinalScore = finalScore,
                EmptyCardPenaltyApplied = emptyCardPenaltyApplied,
                KillsCount = killsCount,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = cell.RowIndex,
                CellColIndex = cell.ColIndex,
                CellTitleSnapshot = cell.Title,
                CellDescriptionSnapshot = cell.Description,
                CellCostSnapshot = cell.Cost,
                CreatedAtUtc = now.AddMinutes(-1),
                UpdatedAtUtc = now
            }
        );
        await db.SaveChangesAsync();
    }

    private async Task ClearGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameTeamFinalResults.RemoveRange(dbContext.GameTeamFinalResults);
        dbContext.GameFinalizations.RemoveRange(dbContext.GameFinalizations);
        dbContext.GameQuizPointLedgerEntries.RemoveRange(dbContext.GameQuizPointLedgerEntries);
        dbContext.GameQuizCorrectAnswers.RemoveRange(dbContext.GameQuizCorrectAnswers);
        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.GameRoundTransitionAudits.RemoveRange(dbContext.GameRoundTransitionAudits);
        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameTeamInvitations.RemoveRange(dbContext.GameTeamInvitations);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.BoardCellMedia.RemoveRange(dbContext.BoardCellMedia);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedUserAsync(Guid userId, string login)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await dbContext.Users.AnyAsync(x => x.Id == userId))
        {
            dbContext.Users.Add(CreateUser(userId, login));
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<Guid> SeedTeamForReadyGameAsync(
        string status,
        int memberCount,
        int slotIndex = 1,
        bool disbandRequested = false
    )
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var slot = await dbContext.GameTeamSlots
            .FirstAsync(slot => slot.GameId == gameId && slot.SlotIndex == slotIndex);
        var utcNow = DateTime.UtcNow;
        var teamId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();

        dbContext.Users.Add(CreateUser(createdByUserId, "team-owner"));
        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slot.Id,
                RecruitmentOpen = status == TeamStatusValue.Forming,
                Status = status,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                ConfirmedAtUtc = status == TeamStatusValue.Confirmed ? utcNow : null,
                ConfirmedByUserId = status == TeamStatusValue.Confirmed ? createdByUserId : null,
                DisbandRequestedAtUtc = disbandRequested ? utcNow : null,
                DisbandRequestedByUserId = disbandRequested ? createdByUserId : null
            }
        );

        for (var index = 0; index < memberCount; index++)
        {
            var userId = index == 0 ? createdByUserId : Guid.NewGuid();
            if (index > 0)
            {
                dbContext.Users.Add(CreateUser(userId, $"team-member-{index}"));
            }

            dbContext.GameTeamMembers.Add(
                new GameTeamMember
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    TeamId = teamId,
                    UserId = userId,
                    JoinedAtUtc = utcNow
                }
            );
        }

        await dbContext.SaveChangesAsync();
        return teamId;
    }

    private async Task SeedPendingInvitationForReadyGameAsync(Guid teamId)
    {
        var gameId = await GetReadyGameIdAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var team = await dbContext.GameTeams.FirstAsync(team => team.Id == teamId);
        var invitedByUserId = team.CreatedByUserId ?? Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        dbContext.Users.Add(CreateUser(invitedUserId, "pending-invite"));
        dbContext.GameTeamInvitations.Add(
            new GameTeamInvitation
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SlotId = team.SlotId,
                TeamId = teamId,
                InvitedByUserId = invitedByUserId,
                InvitedUserId = invitedUserId,
                InvitedByKind = InvitedByKindValue.Admin,
                Status = TeamInvitationStatusValue.Pending,
                CreatedAtUtc = utcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> GetReadyGameIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.Games
            .Where(game => game.Status == GameStatusValue.Ready && !game.IsDeleted)
            .Select(game => game.Id)
            .FirstAsync();
    }

    private static User CreateUser(Guid userId, string login) =>
        new()
        {
            Id = userId,
            TwitchUserId = userId.ToString("N"),
            Login = login,
            DisplayName = login,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private sealed class ThrowingGameLifecycleService : IGameLifecycleService
    {
        public Task<GameLifecycleResult> OpenRegistrationAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameLifecycleResult> StartGameAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameFinishPreviewResult> GetFinishPreviewAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<FinishGameResult> FinishGameAsync(
            Guid gameId,
            FinishGameInput input,
            Guid finishedByUserId,
            CancellationToken cancellationToken = default
        ) =>
            throw new InvalidOperationException("Simulated lifecycle failure.");

        public Task<GameLifecycleResult> ArchiveGameAsync(
            Guid gameId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated lifecycle failure.");
    }
}
