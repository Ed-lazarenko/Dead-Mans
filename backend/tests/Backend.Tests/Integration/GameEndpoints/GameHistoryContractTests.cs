using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameHistoryContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GameHistoryContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLeaderboard_WhenAuthenticated_ReturnsCombinedStatsOrdered()
    {
        await SeedHistoryAsync();
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync("/api/game/history/leaderboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameHistoryLeaderboardEntryDto>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);

        var first = payload[0];
        Assert.Equal("Alpha", first.DisplayName);
        Assert.Equal(100, first.MainGamePoints);
        Assert.Equal(80, first.QuizPoints);
        Assert.Equal(180, first.TotalPoints);
        Assert.Equal(1, first.ModifiersActivated);
        Assert.Equal(1, first.QuizRoundsAnswered);
        Assert.Equal(1, first.CorrectQuizAnswers);

        var second = payload[1];
        Assert.Equal("Bravo", second.DisplayName);
        Assert.Equal(40, second.MainGamePoints);
        Assert.Equal(20, second.QuizPoints);
        Assert.Equal(60, second.TotalPoints);
    }

    [Fact]
    public async Task GetGames_WhenAuthenticated_ReturnsHistorySummaries()
    {
        var seeded = await SeedHistoryAsync();
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync("/api/game/history/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameHistoryGameSummaryDto>>();
        Assert.NotNull(payload);
        var game = Assert.Single(payload);
        Assert.Equal(seeded.GameId.ToString(), game.GameId);
        Assert.Equal("Stabilization Match", game.GameTitle);
        Assert.Equal(GameStatusValue.Finished, game.GameStatus);
        Assert.Equal(2, game.MainGameRunCount);
        Assert.Equal(2, game.QuizRoundCount);
        Assert.Equal(2, game.UniquePlayerCount);
    }

    [Fact]
    public async Task GetGameDetails_WhenAuthenticated_ReturnsSeparatedMainAndQuizHistory()
    {
        var seeded = await SeedHistoryAsync();
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync($"/api/game/history/games/{seeded.GameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(seeded.GameId.ToString(), payload.GameId);
        Assert.Equal(2, payload.MainGame.CardRuns.Count);
        Assert.Single(payload.MainGame.ModifierActivations);
        Assert.Equal(2, payload.MainGame.PlayerStats.Count);
        Assert.Equal("Alpha", payload.MainGame.PlayerStats[0].DisplayName);
        Assert.Equal(100, payload.MainGame.PlayerStats[0].Points);
        Assert.Equal(2, payload.MainGame.PlayerStats[0].EventCount);

        Assert.Equal(2, payload.Quiz.Rounds.Count);
        Assert.Equal(2, payload.Quiz.PlayerStats.Count);
        Assert.Equal("Alpha", payload.Quiz.PlayerStats[0].DisplayName);
        Assert.Equal(80, payload.Quiz.PlayerStats[0].Points);
        Assert.Equal("quiz-001", payload.Quiz.Rounds[0].QuestionCode);
    }

    [Fact]
    public async Task GetGameDetails_WhenActiveGameHasNoHistoryRows_ReturnsEmptySections()
    {
        var gameId = await SeedActiveGameWithoutHistoryAsync();
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync($"/api/game/history/games/{gameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(gameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Active, payload.GameStatus);
        Assert.Empty(payload.MainGame.PlayerStats);
        Assert.Empty(payload.MainGame.ModifierActivations);
        Assert.Empty(payload.MainGame.CardRuns);
        Assert.Empty(payload.Quiz.PlayerStats);
        Assert.Empty(payload.Quiz.Rounds);
    }

    [Fact]
    public async Task GetGameDetails_WhenMissing_ReturnsNotFound()
    {
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync($"/api/game/history/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameLifecycleGameNotFound, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameLifecycleGameNotFound, payload.Code);
    }

    private async Task<Guid> SeedActiveGameWithoutHistoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameCardRunModifierResults.RemoveRange(dbContext.GameCardRunModifierResults);
        dbContext.GameCardRunParticipants.RemoveRange(dbContext.GameCardRunParticipants);
        dbContext.GameCardRuns.RemoveRange(dbContext.GameCardRuns);
        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.GameQuestionSelections.RemoveRange(dbContext.GameQuestionSelections);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.ModifierConflicts.RemoveRange(dbContext.ModifierConflicts);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Active Empty Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now.AddHours(-2),
                ReadyAtUtc = now.AddHours(-1),
                StartedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();

        return gameId;
    }

    private async Task<SeededHistory> SeedHistoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameCardRunModifierResults.RemoveRange(dbContext.GameCardRunModifierResults);
        dbContext.GameCardRunParticipants.RemoveRange(dbContext.GameCardRunParticipants);
        dbContext.GameCardRuns.RemoveRange(dbContext.GameCardRuns);
        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.GameQuestionSelections.RemoveRange(dbContext.GameQuestionSelections);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
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
        var cellOneId = Guid.NewGuid();
        var cellTwoId = Guid.NewGuid();
        var alphaId = Guid.NewGuid();
        var bravoId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var questionOneId = Guid.NewGuid();
        var questionTwoId = Guid.NewGuid();
        var cardRunOneId = Guid.NewGuid();
        var cardRunTwoId = Guid.NewGuid();
        var activationId = Guid.NewGuid();

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
                Title = "Stabilization Match",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = now.AddHours(-3),
                ReadyAtUtc = now.AddHours(-2.5),
                StartedAtUtc = now.AddHours(-2),
                FinishedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 2,
                RowLabels = ["A"],
                ColLabels = ["1", "2"],
                CreatedAtUtc = now.AddHours(-3)
            }
        );

        dbContext.BoardCells.AddRange(
            new BoardCell
            {
                Id = cellOneId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Card One",
                Cost = 100,
                State = BoardCellState.Open
            },
            new BoardCell
            {
                Id = cellTwoId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 1,
                Title = "Card Two",
                Cost = 40,
                State = BoardCellState.Open
            }
        );

        dbContext.ModifierDefinitions.Add(
            new ModifierDefinition
            {
                Id = modifierId,
                Name = "Double Down",
                Description = "Test modifier",
                ScoringType = "conditional_bonus",
                Category = "round",
                ActivationCost = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameActiveModifiers.Add(
            new GameActiveModifier
            {
                Id = activationId,
                GameId = gameId,
                ModifierId = modifierId,
                ActivatedByUserId = alphaId,
                ActivatedAtUtc = now.AddHours(-1.75)
            }
        );

        dbContext.QuestionCategories.Add(
            new QuestionCategory
            {
                Id = categoryId,
                Name = "quiz",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.QuestionDefinitions.AddRange(
            new QuestionDefinition
            {
                Id = questionOneId,
                ExternalCode = "quiz-001",
                CategoryId = categoryId,
                Text = "First quiz question?",
                Answer = "Answer 1",
                NormalizedAnswer = "answer 1",
                Reward = 80,
                Priority = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new QuestionDefinition
            {
                Id = questionTwoId,
                ExternalCode = "quiz-002",
                CategoryId = categoryId,
                Text = "Second quiz question?",
                Answer = "Answer 2",
                NormalizedAnswer = "answer 2",
                Reward = 20,
                Priority = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameCardRuns.AddRange(
            new GameCardRun
            {
                Id = cardRunOneId,
                GameId = gameId,
                BoardCellId = cellOneId,
                TeamId = Guid.NewGuid(),
                Status = GameCardRunStatusValue.Completed,
                StartedAtUtc = now.AddHours(-1.9),
                FinishedAtUtc = now.AddHours(-1.8),
                BaseScore = 100,
                FinalScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Card One",
                CellCostSnapshot = 100,
                ResolvedByUserId = moderatorId,
                CreatedAtUtc = now.AddHours(-1.9),
                UpdatedAtUtc = now.AddHours(-1.8)
            },
            new GameCardRun
            {
                Id = cardRunTwoId,
                GameId = gameId,
                BoardCellId = cellTwoId,
                TeamId = Guid.NewGuid(),
                Status = GameCardRunStatusValue.Completed,
                StartedAtUtc = now.AddHours(-1.7),
                FinishedAtUtc = now.AddHours(-1.6),
                BaseScore = 40,
                FinalScore = 40,
                TeamSlotIndexSnapshot = 2,
                CellRowIndex = 0,
                CellColIndex = 1,
                CellTitleSnapshot = "Card Two",
                CellCostSnapshot = 40,
                ResolvedByUserId = moderatorId,
                CreatedAtUtc = now.AddHours(-1.7),
                UpdatedAtUtc = now.AddHours(-1.6)
            }
        );

        dbContext.GameCardRunParticipants.AddRange(
            new GameCardRunParticipant
            {
                Id = Guid.NewGuid(),
                CardRunId = cardRunOneId,
                UserId = alphaId,
                DisplayNameSnapshot = "Alpha",
                CreatedAtUtc = now.AddHours(-1.9)
            },
            new GameCardRunParticipant
            {
                Id = Guid.NewGuid(),
                CardRunId = cardRunTwoId,
                UserId = bravoId,
                DisplayNameSnapshot = "Bravo",
                CreatedAtUtc = now.AddHours(-1.7)
            }
        );

        dbContext.GameCardRunModifierResults.Add(
            new GameCardRunModifierResult
            {
                Id = Guid.NewGuid(),
                CardRunId = cardRunOneId,
                GameActiveModifierId = activationId,
                ModifierId = modifierId,
                ModifierNameSnapshot = "Double Down",
                ModifierCategorySnapshot = "round",
                ModifierMechanicTypeSnapshot = "multiplier",
                OutcomeStatus = "applied",
                ScoreDelta = 0,
                KillDelta = 0,
                MultiplierApplied = 1.0m,
                ResolvedByUserId = moderatorId,
                ResolvedAtUtc = now.AddHours(-1.8),
                CreatedAtUtc = now.AddHours(-1.8),
                UpdatedAtUtc = now.AddHours(-1.8)
            }
        );

        dbContext.GameQuestionRounds.AddRange(
            new GameQuestionRound
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                QuestionId = questionOneId,
                AskOrder = 1,
                AskedAtUtc = now.AddHours(-1.55),
                AskedByUserId = moderatorId,
                Status = GameQuestionRoundStatusValue.AnsweredCorrect,
                AnsweredAtUtc = now.AddHours(-1.5),
                AnsweredByUserId = moderatorId,
                AnsweredForUserId = alphaId,
                AnsweredByDisplayName = "Alpha",
                SubmittedAnswer = "Answer 1",
                IsCorrect = true,
                AwardedPoints = 80
            },
            new GameQuestionRound
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                QuestionId = questionTwoId,
                AskOrder = 2,
                AskedAtUtc = now.AddHours(-1.45),
                AskedByUserId = moderatorId,
                Status = GameQuestionRoundStatusValue.AnsweredCorrect,
                AnsweredAtUtc = now.AddHours(-1.4),
                AnsweredByUserId = moderatorId,
                AnsweredForUserId = bravoId,
                AnsweredByDisplayName = "Bravo",
                SubmittedAnswer = "Answer 2",
                IsCorrect = true,
                AwardedPoints = 20
            }
        );

        await dbContext.SaveChangesAsync();

        return new SeededHistory(gameId);
    }

    private sealed record SeededHistory(Guid GameId);
}
