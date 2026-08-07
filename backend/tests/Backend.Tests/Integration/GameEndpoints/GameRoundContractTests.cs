using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameRoundContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GameRoundContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Start_WhenRoundWasNotOpenedFirst_ReturnsConflict()
    {
        var seeded = await SeedActiveGameAsync();
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            "/api/game/rounds",
            new StartGameRoundRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundAwaitingModifiersRequired, payload.Code);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.GameRounds.CountAsync());
        Assert.Equal(0, await dbContext.GameRoundParticipants.CountAsync());
        Assert.Equal(0, await dbContext.GameRoundModifierResults.CountAsync());
    }

    [Fact]
    public async Task Start_WhenAnotherRoundAlreadyInProgress_ReturnsConflict()
    {
        var seeded = await SeedActiveGameAsync();
        await SeedInProgressRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            "/api/game/rounds",
            new StartGameRoundRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundAlreadyInProgress, payload.Code);
    }

    [Fact]
    public async Task Start_WhenRoundAwaitingModifiers_TransitionsExistingRoundAndPersistsModifierSnapshots()
    {
        var seeded = await SeedActiveGameAsync();
        var awaitingRoundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            "/api/game/rounds",
            new StartGameRoundRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(awaitingRoundId.ToString(), payload.RoundId);
        Assert.Equal(GameRoundStatusValue.InProgress, payload.Status);
        Assert.Equal(2, payload.Participants.Count);
        Assert.Single(payload.ModifierResults);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.SingleAsync(x => x.Id == awaitingRoundId);
        Assert.Equal(GameRoundStatusValue.InProgress, round.Status);
        Assert.Equal(1, await dbContext.GameRoundModifierResults.CountAsync(x => x.RoundId == awaitingRoundId));
    }

    [Fact]
    public async Task Finalize_WhenAdmin_ReturnsCompletedRoundAndCancelsUnresolvedModifiers()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                null,
                2,
                1,
                "Clean finish",
                [
                    new FinalizeGameRoundModifierRequestDto(
                        started.ModifierResults[0].ModifierResultId,
                        GameRoundModifierOutcomeValue.Completed,
                        30,
                        1,
                        1.5m,
                        "{\"kills\":2}"
                    )
                ]
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameRoundStatusValue.Completed, payload.Status);
        Assert.Equal(510, payload.FinalScore);
        Assert.Equal(2, payload.KillsCount);
        Assert.Equal(1, payload.BountyCount);
        Assert.Equal("Clean finish", payload.Notes);
        Assert.Single(payload.ModifierResults);
        Assert.Equal(GameRoundModifierOutcomeValue.Completed, payload.ModifierResults[0].OutcomeStatus);
        Assert.Equal(30, payload.ModifierResults[0].ScoreDelta);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.SingleAsync();
        Assert.Equal(GameRoundStatusValue.Completed, round.Status);
        Assert.Equal(510, round.FinalScore);
        Assert.Equal(2, round.KillsCount);
        Assert.Equal(1, round.BountyCount);
        Assert.NotNull(round.FinishedAtUtc);
        Assert.Null(await dbContext.Games.Select(x => x.ActiveTeamId).SingleAsync());
        Assert.Equal(3, await dbContext.GameBoards.Select(x => x.Version).SingleAsync());
        var archivedActivation = await dbContext.GameModifierActivations.SingleAsync();
        Assert.NotNull(archivedActivation.ArchivedAtUtc);
    }

    [Fact]
    public async Task Finalize_WhenOutcomeCountsNegative_ReturnsBadRequest()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                null,
                -1,
                0,
                null,
                []
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundInvalidRequest, payload.Code);
    }

    [Fact]
    public async Task Finalize_WhenFinalScoreIsTampered_RecomputesScoreOnServer()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                999_999,
                2,
                1,
                null,
                [
                    new FinalizeGameRoundModifierRequestDto(
                        started.ModifierResults[0].ModifierResultId,
                        GameRoundModifierOutcomeValue.Cancelled,
                        0,
                        0,
                        null,
                        null
                    )
                ]
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(360, payload.FinalScore);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(360, await dbContext.GameRounds.Select(x => x.FinalScore).SingleAsync());
    }

    [Fact]
    public async Task Finalize_WhenCustomAutomaticModifierIsStacked_AppliesAggregateFormulaOnce()
    {
        var seeded = await SeedActiveGameAsync();
        await SeedSecondStackedCustomAutoScoreModifierAsync(seeded);
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        Assert.Equal(2, started.ModifierResults.Count);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                null,
                3,
                0,
                null,
                []
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(390, payload.FinalScore);
        Assert.Equal(30, payload.ModifierResults.Sum(x => x.ScoreDelta));
        Assert.All(
            payload.ModifierResults,
            modifier => Assert.Equal(GameRoundModifierOutcomeValue.Completed, modifier.OutcomeStatus)
        );
        Assert.Equal([15, 15], payload.ModifierResults.Select(x => x.ScoreDelta).Order().ToArray());
    }

    [Fact]
    public async Task Review_WhenRoundInProgress_ReturnsReviewingResultsRound()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);

        var response = await ReviewRoundAsync(seeded, started.RoundId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.RoundId, payload.RoundId);
        Assert.Equal(GameRoundStatusValue.ReviewingResults, payload.Status);
        Assert.Null(payload.FinishedAtUtc);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.SingleAsync();
        Assert.Equal(GameRoundStatusValue.ReviewingResults, round.Status);
        Assert.Null(round.FinishedAtUtc);
    }

    [Fact]
    public async Task GetActive_WhenRoundExists_ReturnsCurrentInProgressRound()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.GetAsync("/api/game/rounds/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.RoundId, payload.RoundId);
        Assert.Equal(GameRoundStatusValue.InProgress, payload.Status);
    }

    [Fact]
    public async Task GetActive_WhenRoundAwaitingModifiers_ReturnsCurrentRound()
    {
        var seeded = await SeedActiveGameAsync();
        var awaitingRoundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: Guid.NewGuid()
        );

        var response = await client.GetAsync("/api/game/rounds/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(awaitingRoundId.ToString(), payload.RoundId);
        Assert.Equal(GameRoundStatusValue.AwaitingModifiers, payload.Status);
        Assert.Empty(payload.ModifierResults);
    }

    [Fact]
    public async Task GetActive_WhenRoundReviewingResults_ReturnsCurrentRound()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: Guid.NewGuid()
        );

        var response = await client.GetAsync("/api/game/rounds/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.RoundId, payload.RoundId);
        Assert.Equal(GameRoundStatusValue.ReviewingResults, payload.Status);
    }

    [Fact]
    public async Task GetActive_WhenViewer_ReturnsCurrentInProgressRound()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: Guid.NewGuid()
        );

        var response = await client.GetAsync("/api/game/rounds/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.RoundId, payload.RoundId);
        Assert.Equal(GameRoundStatusValue.InProgress, payload.Status);
    }

    [Fact]
    public async Task GetEligibleTeams_WhenModerator_ReturnsConfirmedTeamsWithParticipants()
    {
        var seeded = await SeedActiveGameAsync();
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.GetAsync("/api/game/rounds/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameRoundTeamOptionDto>>();
        Assert.NotNull(payload);
        var team = Assert.Single(payload);
        Assert.Equal(seeded.TeamId.ToString(), team.TeamId);
        Assert.Equal(1, team.TeamSlotIndex);
        Assert.Equal(2, team.Participants.Count);
    }

    private async Task<HttpResponseMessage> StartRoundAsync(SeededActiveGame seeded)
    {
        await SeedAwaitingModifiersRoundAsync(seeded);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        return await client.PostAsJsonAsync(
            "/api/game/rounds",
            new StartGameRoundRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );
    }

    private async Task<HttpResponseMessage> ReviewRoundAsync(SeededActiveGame seeded, string roundId)
    {
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        return await client.PostAsync($"/api/game/rounds/{roundId}/review", content: null);
    }

    private async Task SeedSecondStackedCustomAutoScoreModifierAsync(SeededActiveGame seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await dbContext.ModifierDefinitions.SingleAsync();
        modifier.ScoringType = GameModifierScoringTypes.ConditionalBonusPenalty;
        modifier.Category = GameModifierCategories.Result;
        modifier.MetadataJson =
            """
            {"effect":{"mechanicType":"restriction_with_reward","traits":["requires_manual_resolution"],"durationSeconds":null,"ruleText":null,"scoreImpact":{"pointsDelta":null,"perKillBonus":5,"failurePenaltyPoints":25,"multiplierDelta":null,"killDelta":null,"scoreFormula":{"mode":"custom_expression","successExpression":"killsCount * perKillBonus * activationCount","failureExpression":null}},"conditions":[{"type":"at_least_one_kill","source":"manual_input"}],"resolutionInputs":["kills"],"killEffect":null,"multiplierEffect":null,"mentorEffect":null}}
            """;
        modifier.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                ModifierId = modifier.Id,
                ActivatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = modifier.ActivationCost,
                ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-9)
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<SeededActiveGame> SeedActiveGameAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.GameEnabledQuestions.RemoveRange(dbContext.GameEnabledQuestions);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.ModifierConflicts.RemoveRange(dbContext.ModifierConflicts);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var alphaId = Guid.NewGuid();
        var bravoId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var activeModifierId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = alphaId,
                TwitchUserId = "alpha-user",
                Login = "alpha",
                DisplayName = "Alpha",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = bravoId,
                TwitchUserId = "bravo-user",
                Login = "bravo",
                DisplayName = "Bravo",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = moderatorId,
                TwitchUserId = "mod-user",
                Login = "mod",
                DisplayName = "Moderator",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Runtime Match",
                Status = GameStatusValue.Active,
                ActiveTeamId = teamId,
                CreatedAtUtc = now.AddHours(-1),
                ReadyAtUtc = now.AddMinutes(-50),
                StartedAtUtc = now.AddMinutes(-40)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                Version = 2,
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Main Card",
                Cost = 120,
                State = BoardCellState.Open
            }
        );

        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = "open",
                CreatedAtUtc = now.AddMinutes(-55)
            }
        );

        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = moderatorId,
                ConfirmedByUserId = moderatorId,
                ConfirmedAtUtc = now.AddMinutes(-30),
                CreatedAtUtc = now.AddMinutes(-35),
                UpdatedAtUtc = now.AddMinutes(-30)
            }
        );

        dbContext.GameTeamMembers.AddRange(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = alphaId,
                JoinedAtUtc = now.AddMinutes(-34)
            },
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = bravoId,
                JoinedAtUtc = now.AddMinutes(-33)
            }
        );

        dbContext.ModifierDefinitions.Add(
            new ModifierDefinition
            {
                Id = modifierId,
                Name = "Momentum",
                Description = "Bonus score modifier",
                ScoringType = "conditional_bonus",
                Category = "round",
                MetadataJson = "{\"effect\":{\"mechanicType\":\"multiplier\"}}",
                ActivationCost = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = activeModifierId,
                GameId = gameId,
                ModifierId = modifierId,
                ActivatedByUserId = moderatorId,
                ActivationCostSnapshot = 5,
                ActivatedAtUtc = now.AddMinutes(-10)
            }
        );

        await dbContext.SaveChangesAsync();

        return new SeededActiveGame(gameId, cellId, teamId, moderatorId);
    }

    private async Task SeedInProgressRoundAsync(SeededActiveGame seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                BoardCellId = seeded.CellId,
                TeamId = seeded.TeamId,
                Status = GameRoundStatusValue.InProgress,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                BaseScore = 120,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Main Card",
                CellCostSnapshot = 120,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedAwaitingModifiersRoundAsync(SeededActiveGame seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow.AddMinutes(-5);
        var roundId = Guid.NewGuid();
        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = seeded.GameId,
                BoardCellId = seeded.CellId,
                TeamId = seeded.TeamId,
                Status = GameRoundStatusValue.AwaitingModifiers,
                StartedAtUtc = now,
                BaseScore = 120,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Main Card",
                CellCostSnapshot = 120,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        var participants = await dbContext.GameTeamMembers
            .AsNoTracking()
            .Where(x => x.GameId == seeded.GameId && x.TeamId == seeded.TeamId)
            .OrderBy(x => x.JoinedAtUtc)
            .Select(x => new
            {
                x.UserId,
                DisplayName = x.User != null ? x.User.DisplayName : string.Empty
            })
            .ToArrayAsync();
        dbContext.GameRoundParticipants.AddRange(
            participants.Select(
                participant =>
                    new GameRoundParticipant
                    {
                        Id = Guid.NewGuid(),
                        RoundId = roundId,
                        UserId = participant.UserId,
                        DisplayNameSnapshot = string.IsNullOrWhiteSpace(participant.DisplayName)
                            ? participant.UserId.ToString()
                            : participant.DisplayName,
                        CreatedAtUtc = now
                    }
            )
        );

        await dbContext.SaveChangesAsync();
        return roundId;
    }

    private sealed record SeededActiveGame(Guid GameId, Guid CellId, Guid TeamId, Guid ModeratorId);
}
