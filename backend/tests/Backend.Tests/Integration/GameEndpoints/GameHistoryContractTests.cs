using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
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
        _factory.ResetDatabase();
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
        Assert.Equal(2, game.MainGameRoundCount);
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
        Assert.Null(payload.FinalResult);
        Assert.Equal(2, payload.MainGame.Rounds.Count);
        Assert.Single(payload.MainGame.ModifierActivations);
        Assert.Equal(2, payload.MainGame.PlayerStats.Count);
        Assert.Equal("Alpha", payload.MainGame.PlayerStats[0].DisplayName);
        Assert.Equal(100, payload.MainGame.PlayerStats[0].Points);
        Assert.Equal(2, payload.MainGame.PlayerStats[0].EventCount);
        Assert.Equal(seeded.CellOneId.ToString(), payload.MainGame.Rounds[0].CellId);
        Assert.Equal("question", payload.MainGame.Rounds[0].CellType);
        Assert.Equal("Archived primary extraction route", payload.MainGame.Rounds[0].CellDescription);
        Assert.False(payload.MainGame.Rounds[0].EmptyCardPenaltyApplied);
        Assert.Single(payload.MainGame.Rounds[0].CellMedia);
        Assert.Equal(
            "http://localhost:9000/game-media/cards/card-one-archived.png",
            payload.MainGame.Rounds[0].CellMedia[0].Url
        );
        var roundModifier = Assert.Single(payload.MainGame.Rounds[0].Modifiers);
        Assert.Equal(
            "{\"source\":\"round_kills\",\"effect\":\"success\",\"activationCount\":1}",
            roundModifier.ResolutionDataJson
        );

        Assert.Equal(2, payload.Quiz.Rounds.Count);
        Assert.Equal(2, payload.Quiz.PlayerStats.Count);
        Assert.Equal("Alpha", payload.Quiz.PlayerStats[0].DisplayName);
        Assert.Equal(80, payload.Quiz.PlayerStats[0].Points);
        Assert.DoesNotContain(payload.Quiz.PlayerStats, item => item.DisplayName == "Moderator");
        Assert.Equal("quiz-001", payload.Quiz.Rounds[0].QuestionCode);
        Assert.Equal("Moderator", payload.Quiz.Rounds[0].AnsweredByDisplayName);
        Assert.Equal(seeded.AlphaId.ToString(), payload.Quiz.Rounds[0].AnsweredForUserId);
        Assert.Equal("Alpha", payload.Quiz.Rounds[0].AnsweredForDisplayName);
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
        Assert.Empty(payload.MainGame.Rounds);
        Assert.Empty(payload.Quiz.PlayerStats);
        Assert.Empty(payload.Quiz.Rounds);
    }

    [Fact]
    public async Task GetGameDetails_WhenRoundIsNotTerminal_DoesNotExposeDraftOrCountIt()
    {
        var seeded = await SeedHistoryAsync();
        var activeRoundId = await SeedNonTerminalRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync($"/api/game/history/games/{seeded.GameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(payload);

        Assert.DoesNotContain(
            payload.MainGame.Rounds,
            round => round.RoundId == activeRoundId.ToString()
        );

        Assert.Equal(2, payload.MainGame.TeamStats.Count);
        var alpha = Assert.Single(
            payload.MainGame.PlayerStats,
            player => player.UserId == seeded.AlphaId.ToString()
        );
        Assert.Equal(100, alpha.Points);
        Assert.Equal(2, alpha.EventCount);
    }

    [Fact]
    public async Task GetGameDetails_WhenRoundWasTechnicallyCancelled_ExposesAuditButNotLeaderboard()
    {
        var seeded = await SeedHistoryAsync();
        var cancelledRoundId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sourceRound = await dbContext.GameRounds
                .AsNoTracking()
                .FirstAsync(round => round.GameId == seeded.GameId);
            var now = DateTime.UtcNow;
            dbContext.GameRounds.Add(
                new GameRound
                {
                    Id = cancelledRoundId,
                    GameId = seeded.GameId,
                    BoardId = sourceRound.BoardId,
                    BoardCellId = sourceRound.BoardCellId,
                    TeamId = sourceRound.TeamId,
                    Status = GameRoundStatusValue.Cancelled,
                    Version = 5,
                    GameplayStartedAtUtc = now.AddMinutes(-4),
                    FinishedAtUtc = now,
                    BaseScore = 100,
                    FinalScore = 0,
                    TeamSlotIndexSnapshot = sourceRound.TeamSlotIndexSnapshot,
                    CellRowIndex = sourceRound.CellRowIndex,
                    CellColIndex = sourceRound.CellColIndex,
                    CellTitleSnapshot = "Cancelled card",
                    CellCostSnapshot = 100,
                    TechnicalCancellationReasonCode =
                        GameRoundTechnicalCancellationReasonValue.StreamOrInfrastructureFailure,
                    PublicCancellationSummary = "Stream unavailable.",
                    CreatedAtUtc = now.AddMinutes(-5),
                    UpdatedAtUtc = now
                }
            );
            dbContext.GameRoundTransitionAudits.Add(
                new GameRoundTransitionAudit
                {
                    RoundId = cancelledRoundId,
                    Sequence = 1,
                    FromStatus = GameRoundStatusValue.InProgress,
                    ToStatus = GameRoundStatusValue.Cancelled,
                    ActionCode = GameRoundTransitionActionValue.TechnicalCancel,
                    InitiatedByUserId = seeded.AlphaId,
                    OccurredAtUtc = now,
                    ResultingRoundVersion = 5
                }
            );
            await dbContext.SaveChangesAsync();
        }

        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);
        var response = await client.GetAsync($"/api/game/history/games/{seeded.GameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(payload);
        var cancelled = Assert.Single(
            payload.MainGame.Rounds,
            round => round.RoundId == cancelledRoundId.ToString()
        );
        Assert.Equal(GameRoundStatusValue.Cancelled, cancelled.Status);
        Assert.Equal(GameRoundStatusValue.InProgress, cancelled.TechnicalCancellationStage);
        Assert.Equal(
            GameRoundTechnicalCancellationReasonValue.StreamOrInfrastructureFailure,
            cancelled.TechnicalCancellationReasonCode
        );
        Assert.Equal("Stream unavailable.", cancelled.PublicCancellationSummary);
        Assert.True(cancelled.PurchasesRefunded);
        Assert.DoesNotContain(
            payload.MainGame.TeamStats.SelectMany(team => team.Rounds),
            round => round.RoundId == cancelledRoundId.ToString()
        );
        Assert.Equal(2, payload.MainGame.TeamStats.Count);
    }

    [Fact]
    public async Task GetGameDetails_WhenPointTotalsExceedContractRange_ClampsWithoutOverflow()
    {
        var seeded = await SeedHistoryAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var firstBalance = 80L;
            var secondBalance = firstBalance + int.MaxValue;
            dbContext.GameQuizPointLedgerEntries.AddRange(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = seeded.GameId,
                    UserId = seeded.AlphaId,
                    EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                    PointsDelta = int.MaxValue,
                    ManualRequestId = Guid.NewGuid(),
                    CreatedByUserId = seeded.AlphaId,
                    Reason = "Overflow boundary one",
                    AvailablePointsBefore = firstBalance,
                    AvailablePointsAfter = secondBalance,
                    OccurredAtUtc = now
                },
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = seeded.GameId,
                    UserId = seeded.AlphaId,
                    EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                    PointsDelta = int.MaxValue,
                    ManualRequestId = Guid.NewGuid(),
                    CreatedByUserId = seeded.AlphaId,
                    Reason = "Overflow boundary two",
                    AvailablePointsBefore = secondBalance,
                    AvailablePointsAfter = secondBalance + int.MaxValue,
                    OccurredAtUtc = now.AddSeconds(1)
                }
            );
            await dbContext.SaveChangesAsync();
        }
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var details = await client.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{seeded.GameId}"
        );
        var leaderboard = await client.GetFromJsonAsync<IReadOnlyList<GameHistoryLeaderboardEntryDto>>(
            "/api/game/history/leaderboard"
        );

        Assert.NotNull(details);
        Assert.Equal(int.MaxValue, details.Quiz.TotalPoints);
        var alphaQuiz = Assert.Single(
            details.Quiz.PlayerStats,
            player => player.UserId == seeded.AlphaId.ToString()
        );
        Assert.Equal(int.MaxValue, alphaQuiz.Points);

        Assert.NotNull(leaderboard);
        var alphaTotal = Assert.Single(
            leaderboard,
            player => player.UserId == seeded.AlphaId.ToString()
        );
        Assert.Equal(int.MaxValue, alphaTotal.QuizPoints);
        Assert.Equal(int.MaxValue, alphaTotal.TotalPoints);
    }

    [Fact]
    public async Task GetGameDetails_WhenTeamHasPenalties_RanksByFinalTeamScore()
    {
        var gameId = await SeedTeamLeaderboardScoreFormulaAsync();
        using var client = TestAuthClientFactory.CreateClient(_factory, [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync($"/api/game/history/games/{gameId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.MainGame.TeamStats.Count);

        var leader = payload.MainGame.TeamStats[0];
        Assert.Equal(2, leader.TeamSlotIndex);
        Assert.Equal(650, leader.BestScore);
        Assert.Equal(0, leader.PenaltyTotal);
        Assert.Equal(650, leader.FinalScore);
        Assert.Equal(1, leader.TotalKills);

        var penalizedTeam = payload.MainGame.TeamStats[1];
        Assert.Equal(1, penalizedTeam.TeamSlotIndex);
        Assert.Equal(800, penalizedTeam.BestScore);
        Assert.Equal(300, penalizedTeam.PenaltyTotal);
        Assert.Equal(500, penalizedTeam.FinalScore);
        Assert.Equal(4, penalizedTeam.RoundsPlayed);
        Assert.Equal(1, penalizedTeam.TotalKills);
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

        await ClearHistoryTestDataAsync(dbContext);

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

    private async Task<Guid> SeedNonTerminalRoundAsync(SeededHistory seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var completedRound = await dbContext.GameRounds
            .AsNoTracking()
            .FirstAsync(x => x.GameId == seeded.GameId && x.Status == GameRoundStatusValue.Completed);
        var now = DateTime.UtcNow;
        var roundId = Guid.NewGuid();

        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = seeded.GameId,
                BoardId = completedRound.BoardId,
                BoardCellId = seeded.CellOneId,
                TeamId = completedRound.TeamId,
                Status = GameRoundStatusValue.ReviewingResults,
                BaseScore = 500,
                FinalScore = null,
                TeamSlotIndexSnapshot = completedRound.TeamSlotIndexSnapshot,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Unfinished card",
                CellCostSnapshot = 500,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.GameRoundParticipants.Add(
            new GameRoundParticipant
            {
                Id = Guid.NewGuid(),
                RoundId = roundId,
                UserId = seeded.AlphaId,
                DisplayNameSnapshot = "Alpha",
                CreatedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();

        return roundId;
    }

    private async Task<Guid> SeedTeamLeaderboardScoreFormulaAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearHistoryTestDataAsync(dbContext);

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var teamOneId = Guid.NewGuid();
        var teamTwoId = Guid.NewGuid();
        var teamOnePlayerId = Guid.NewGuid();
        var teamTwoPlayerId = Guid.NewGuid();
        var cellIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Penalty Formula Match",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now.AddHours(-3),
                ReadyAtUtc = now.AddHours(-2.5),
                StartedAtUtc = now.AddHours(-2)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = cellIds.Length,
                RowLabels = ["A"],
                ColLabels = ["1", "2", "3", "4", "5"],
                CreatedAtUtc = now.AddHours(-3)
            }
        );

        dbContext.BoardCells.AddRange(
            cellIds.Select(
                (cellId, index) =>
                    new BoardCell
                    {
                        Id = cellId,
                        BoardId = boardId,
                        RowIndex = 0,
                        ColIndex = index,
                        CellType = "question",
                        Title = $"Card {index + 1}",
                        Cost = index == 4 ? 650 : 100,
                        State = BoardCellState.Open
                    }
            )
        );

        var rounds = new[]
        {
            CreateCompletedRound(
                gameId,
                boardId,
                cellIds[0],
                teamOneId,
                1,
                now.AddMinutes(-50),
                100,
                0,
                -100
            ),
            CreateCompletedRound(
                gameId,
                boardId,
                cellIds[1],
                teamOneId,
                1,
                now.AddMinutes(-40),
                100,
                0,
                -100
            ),
            CreateCompletedRound(
                gameId,
                boardId,
                cellIds[2],
                teamOneId,
                1,
                now.AddMinutes(-30),
                100,
                0,
                -100
            ),
            CreateCompletedRound(
                gameId,
                boardId,
                cellIds[3],
                teamOneId,
                1,
                now.AddMinutes(-20),
                800,
                1,
                800
            ),
            CreateCompletedRound(
                gameId,
                boardId,
                cellIds[4],
                teamTwoId,
                2,
                now.AddMinutes(-10),
                650,
                1,
                650
            )
        };

        dbContext.GameRounds.AddRange(rounds);
        dbContext.GameRoundParticipants.AddRange(
            rounds.Where(x => x.TeamId == teamOneId)
                .Select(
                    x =>
                        new GameRoundParticipant
                        {
                            Id = Guid.NewGuid(),
                            RoundId = x.Id,
                            UserId = teamOnePlayerId,
                            DisplayNameSnapshot = "Penalty Crew",
                            CreatedAtUtc = x.CreatedAtUtc
                        }
                )
                .Concat(
                    rounds.Where(x => x.TeamId == teamTwoId)
                        .Select(
                            x =>
                                new GameRoundParticipant
                                {
                                    Id = Guid.NewGuid(),
                                    RoundId = x.Id,
                                    UserId = teamTwoPlayerId,
                                    DisplayNameSnapshot = "Clean Crew",
                                    CreatedAtUtc = x.CreatedAtUtc
                                }
                        )
                )
        );

        await dbContext.SaveChangesAsync();
        return gameId;
    }

    private static GameRound CreateCompletedRound(
        Guid gameId,
        Guid boardId,
        Guid cellId,
        Guid teamId,
        int teamSlotIndex,
        DateTime startedAtUtc,
        int baseScore,
        int killsCount,
        int finalScore
    )
    {
        return new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            BoardId = boardId,
            BoardCellId = cellId,
            TeamId = teamId,
            Status = GameRoundStatusValue.Completed,
            FinishedAtUtc = startedAtUtc.AddMinutes(5),
            BaseScore = baseScore,
            FinalScore = finalScore,
            EmptyCardPenaltyApplied = finalScore < 0,
            KillsCount = killsCount,
            TeamSlotIndexSnapshot = teamSlotIndex,
            CellRowIndex = 0,
            CellColIndex = teamSlotIndex - 1,
            CellTitleSnapshot = $"Team {teamSlotIndex} card",
            CellCostSnapshot = baseScore,
            CreatedAtUtc = startedAtUtc,
            UpdatedAtUtc = startedAtUtc.AddMinutes(5)
        };
    }

    private static async Task ClearHistoryTestDataAsync(ApplicationDbContext dbContext)
    {
        dbContext.GameQuizPointLedgerEntries.RemoveRange(dbContext.GameQuizPointLedgerEntries);
        dbContext.GameQuizCorrectAnswers.RemoveRange(dbContext.GameQuizCorrectAnswers);
        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.GameEnabledQuestions.RemoveRange(dbContext.GameEnabledQuestions);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        dbContext.BoardCellMedia.RemoveRange(dbContext.BoardCellMedia);
        dbContext.MediaAssets.RemoveRange(dbContext.MediaAssets);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();
    }

    private async Task<SeededHistory> SeedHistoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearHistoryTestDataAsync(dbContext);

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
        var roundOneId = Guid.NewGuid();
        var roundTwoId = Guid.NewGuid();
        var activationId = Guid.NewGuid();
        var mediaAssetId = Guid.NewGuid();
        var quizRoundOneId = Guid.NewGuid();
        var quizRoundTwoId = Guid.NewGuid();
        var correctAnswerOneId = Guid.NewGuid();
        var correctAnswerTwoId = Guid.NewGuid();

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
                CellType = "question",
                Title = "Card One",
                Description = "Current live board description",
                Cost = 100,
                State = BoardCellState.Open
            },
            new BoardCell
            {
                Id = cellTwoId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 1,
                CellType = "question",
                Title = "Card Two",
                Description = "Secondary flank route",
                Cost = 40,
                State = BoardCellState.Open
            }
        );

        dbContext.MediaAssets.Add(
            new MediaAsset
            {
                Id = mediaAssetId,
                Bucket = "history-tests",
                ObjectKey = "cards/card-one-current.png",
                MimeType = "image/png",
                SizeBytes = 128,
                CreatedAtUtc = now
            }
        );

        dbContext.BoardCellMedia.Add(
            new BoardCellMedia
            {
                Id = Guid.NewGuid(),
                CellId = cellOneId,
                MediaAssetId = mediaAssetId,
                Role = "content",
                SortOrder = 0
            }
        );

        await TestModifierVersionFactory.AddAsync(
            dbContext,
            new TestModifierSpec(
                modifierId,
                "Double Down",
                "Test modifier",
                "round",
                5,
                null,
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior),
            now);

        dbContext.GameModifierActivations.Add(
            new GameModifierActivation
            {
                Id = activationId,
                GameId = gameId,
                RoundId = roundOneId,
                ModifierId = modifierId,
                ActivatedByUserId = alphaId,
                InitiatedByUserId = alphaId,
                ActivatedAtUtc = now.AddHours(-1.75),
                Status = GameModifierActivationStatusValue.Consumed,
                ArchivedAtUtc = now.AddHours(-1.7)
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
                Reward = 80,
                Priority = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                AcceptedAnswers =
                [
                    new QuestionAcceptedAnswer
                    {
                        Id = Guid.NewGuid(),
                        AnswerText = "Answer 1",
                        NormalizedAnswer = "answer 1",
                        IsPrimary = true,
                        SortOrder = 0,
                        CreatedAtUtc = now
                    }
                ]
            },
            new QuestionDefinition
            {
                Id = questionTwoId,
                ExternalCode = "quiz-002",
                CategoryId = categoryId,
                Text = "Second quiz question?",
                Reward = 20,
                Priority = 2,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                AcceptedAnswers =
                [
                    new QuestionAcceptedAnswer
                    {
                        Id = Guid.NewGuid(),
                        AnswerText = "Answer 2",
                        NormalizedAnswer = "answer 2",
                        IsPrimary = true,
                        SortOrder = 0,
                        CreatedAtUtc = now
                    }
                ]
            }
        );

        dbContext.GameRounds.AddRange(
            new GameRound
            {
                Id = roundOneId,
                GameId = gameId,
                BoardId = boardId,
                BoardCellId = cellOneId,
                TeamId = Guid.NewGuid(),
                Status = GameRoundStatusValue.Completed,
                FinishedAtUtc = now.AddHours(-1.8),
                BaseScore = 100,
                FinalScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Card One",
                CellDescriptionSnapshot = "Archived primary extraction route",
                CellCostSnapshot = 100,
                ResolvedByUserId = moderatorId,
                CreatedAtUtc = now.AddHours(-1.9),
                UpdatedAtUtc = now.AddHours(-1.8)
            },
            new GameRound
            {
                Id = roundTwoId,
                GameId = gameId,
                BoardId = boardId,
                BoardCellId = cellTwoId,
                TeamId = Guid.NewGuid(),
                Status = GameRoundStatusValue.Completed,
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

        dbContext.GameRoundCellMedia.Add(
            new GameRoundCellMedia
            {
                Id = Guid.NewGuid(),
                RoundId = roundOneId,
                Bucket = "game-media",
                ObjectKey = "cards/card-one-archived.png",
                MimeType = "image/png",
                SizeBytes = 1,
                Role = "image",
                SortOrder = 0,
                CreatedAtUtc = now.AddHours(-1.9)
            }
        );

        dbContext.GameRoundParticipants.AddRange(
            new GameRoundParticipant
            {
                Id = Guid.NewGuid(),
                RoundId = roundOneId,
                UserId = alphaId,
                DisplayNameSnapshot = "Alpha",
                CreatedAtUtc = now.AddHours(-1.9)
            },
            new GameRoundParticipant
            {
                Id = Guid.NewGuid(),
                RoundId = roundTwoId,
                UserId = bravoId,
                DisplayNameSnapshot = "Bravo",
                CreatedAtUtc = now.AddHours(-1.7)
            }
        );

        dbContext.GameRoundModifierResults.Add(
            new GameRoundModifierResult
            {
                Id = Guid.NewGuid(),
                RoundId = roundOneId,
                GameModifierActivationId = activationId,
                ModifierId = modifierId,
                ModifierNameSnapshot = "Double Down",
                ModifierCategorySnapshot = "round",
                OutcomeStatus = "applied",
                ScoreDelta = 0,
                KillDelta = 0,
                MultiplierApplied = 1.0m,
                ResolutionDataJson =
                    "{\"source\":\"round_kills\",\"effect\":\"success\",\"activationCount\":1}",
                ResolvedByUserId = moderatorId,
                ResolvedAtUtc = now.AddHours(-1.8),
                CreatedAtUtc = now.AddHours(-1.8),
                UpdatedAtUtc = now.AddHours(-1.8)
            }
        );

        dbContext.GameQuizRounds.AddRange(
            new GameQuizRound
            {
                Id = quizRoundOneId,
                GameId = gameId,
                QuestionId = questionOneId,
                AskOrder = 1,
                AskedAtUtc = now.AddHours(-1.55),
                ClosesAtUtc = now.AddHours(-1.45),
                ClosedAtUtc = now.AddHours(-1.5),
                AskedByUserId = moderatorId,
                Status = GameQuizRoundStatusValue.AnsweredCorrect,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = "quiz-001",
                CategoryNameSnapshot = "quiz",
                QuestionTextSnapshot = "First quiz question?",
                AcceptedAnswersSnapshot = ["Answer 1"],
                NormalizedAnswersSnapshot = ["answer 1"],
                RewardSnapshot = 80,
                DeliveryKind = "manual"
            },
            new GameQuizRound
            {
                Id = quizRoundTwoId,
                GameId = gameId,
                QuestionId = questionTwoId,
                AskOrder = 2,
                AskedAtUtc = now.AddHours(-1.45),
                ClosesAtUtc = now.AddHours(-1.35),
                ClosedAtUtc = now.AddHours(-1.4),
                AskedByUserId = moderatorId,
                Status = GameQuizRoundStatusValue.AnsweredCorrect,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = "quiz-002",
                CategoryNameSnapshot = "quiz",
                QuestionTextSnapshot = "Second quiz question?",
                AcceptedAnswersSnapshot = ["Answer 2"],
                NormalizedAnswersSnapshot = ["answer 2"],
                RewardSnapshot = 20,
                DeliveryKind = "manual"
            }
        );

        dbContext.GameQuizCorrectAnswers.AddRange(
            new GameQuizCorrectAnswer
            {
                Id = correctAnswerOneId,
                GameId = gameId,
                QuizRoundId = quizRoundOneId,
                AwardedToUserId = alphaId,
                CapturedByUserId = moderatorId,
                TwitchUserIdSnapshot = "alpha-user",
                LoginSnapshot = "alpha",
                DisplayNameSnapshot = "Alpha",
                SubmittedAnswer = "Answer 1",
                NormalizedAnswer = "answer 1",
                SourceProvider = "manual",
                AnsweredAtUtc = now.AddHours(-1.5)
            },
            new GameQuizCorrectAnswer
            {
                Id = correctAnswerTwoId,
                GameId = gameId,
                QuizRoundId = quizRoundTwoId,
                AwardedToUserId = bravoId,
                CapturedByUserId = moderatorId,
                TwitchUserIdSnapshot = "bravo-user",
                LoginSnapshot = "bravo",
                DisplayNameSnapshot = "Bravo",
                SubmittedAnswer = "Answer 2",
                NormalizedAnswer = "answer 2",
                SourceProvider = "manual",
                AnsweredAtUtc = now.AddHours(-1.4)
            }
        );

        dbContext.GameQuizPointLedgerEntries.AddRange(
            new GameQuizPointLedgerEntry
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                UserId = alphaId,
                EntryType = GameQuizPointEntryTypeValue.QuizReward,
                PointsDelta = 80,
                CorrectAnswerId = correctAnswerOneId,
                AvailablePointsBefore = 0,
                AvailablePointsAfter = 80,
                OccurredAtUtc = now.AddHours(-1.5)
            },
            new GameQuizPointLedgerEntry
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                UserId = bravoId,
                EntryType = GameQuizPointEntryTypeValue.QuizReward,
                PointsDelta = 20,
                CorrectAnswerId = correctAnswerTwoId,
                AvailablePointsBefore = 0,
                AvailablePointsAfter = 20,
                OccurredAtUtc = now.AddHours(-1.4)
            }
        );

        await dbContext.SaveChangesAsync();

        return new SeededHistory(gameId, alphaId, cellOneId);
    }

    private sealed record SeededHistory(Guid GameId, Guid AlphaId, Guid CellOneId);
}
