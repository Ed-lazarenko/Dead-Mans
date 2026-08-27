using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
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
        using (var mutationScope = _factory.Services.CreateScope())
        {
            var mutationDb = mutationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var activation = await mutationDb.GameModifierActivations.SingleAsync(
                x => x.RoundId == awaitingRoundId
            );
            activation.BehaviorV2SnapshotJson = ModifierBehaviorV2Json.Serialize(
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Hard75).Behavior
            );
            var liveDefinition = await mutationDb.ModifierDefinitions.SingleAsync();
            liveDefinition.Name = "Mutated live catalog name";
            liveDefinition.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior
            );
            await mutationDb.SaveChangesAsync();
        }
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
        Assert.Equal("Momentum", Assert.Single(payload.ModifierResults).ModifierName);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.SingleAsync(x => x.Id == awaitingRoundId);
        Assert.Equal(GameRoundStatusValue.InProgress, round.Status);
        var result = await dbContext.GameRoundModifierResults.SingleAsync(
            x => x.RoundId == awaitingRoundId
        );
        Assert.Equal(1, result.DefinitionRevisionSnapshot);
        Assert.NotNull(result.ModifierBehaviorV2SnapshotJson);
        Assert.Equal(
            ModifierBehaviorSchemaVersions.V2,
            ModifierBehaviorV2Json.Deserialize(result.ModifierBehaviorV2SnapshotJson).SchemaVersion
        );
        Assert.Equal(
            ModifierFormulaCodes.CardPercentPerUnit,
            ModifierBehaviorV2Json.Deserialize(result.ModifierBehaviorV2SnapshotJson)
                .FormulaReference?.Code
        );
    }

    [Fact]
    public async Task VersionedLifecycle_PrepareBeginReviewAndResume_PreservesOriginalGameplayTimeline()
    {
        var seeded = await SeedActiveGameAsync();
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var prepareResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/prepare",
            new GameRoundVersionCommandRequestDto(1)
        );
        var prepared = await prepareResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        Assert.NotNull(prepared);
        Assert.Equal(GameRoundStatusValue.Preparing, prepared.Status);
        Assert.Equal(2, prepared.RoundVersion);
        Assert.NotNull(prepared.PreparedAtUtc);
        Assert.Null(prepared.GameplayStartedAtUtc);
        Assert.Empty(prepared.ModifierResults);

        var staleBeginResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/begin-gameplay",
            new GameRoundVersionCommandRequestDto(1)
        );
        var staleError = await staleBeginResponse.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.Conflict, staleBeginResponse.StatusCode);
        Assert.NotNull(staleError);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundStaleVersion, staleError.Code);

        var beginResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/begin-gameplay",
            new GameRoundVersionCommandRequestDto(prepared.RoundVersion)
        );
        var inProgress = await beginResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);
        Assert.NotNull(inProgress);
        Assert.Equal(GameRoundStatusValue.InProgress, inProgress.Status);
        Assert.Equal(3, inProgress.RoundVersion);
        Assert.NotNull(inProgress.GameplayStartedAtUtc);
        Assert.Single(inProgress.ModifierResults);

        var repeatedBeginResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/begin-gameplay",
            new GameRoundVersionCommandRequestDto(prepared.RoundVersion)
        );
        var repeatedBegin = await repeatedBeginResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, repeatedBeginResponse.StatusCode);
        Assert.NotNull(repeatedBegin);
        Assert.Equal(inProgress.RoundVersion, repeatedBegin.RoundVersion);
        Assert.Single(repeatedBegin.ModifierResults);

        var reviewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/review",
            new GameRoundVersionCommandRequestDto(inProgress.RoundVersion)
        );
        var reviewing = await reviewResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        Assert.NotNull(reviewing);
        Assert.Equal(GameRoundStatusValue.ReviewingResults, reviewing.Status);
        Assert.Equal(4, reviewing.RoundVersion);
        Assert.NotNull(reviewing.ReviewedAtUtc);

        var resumeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/resume-gameplay",
            new GameRoundVersionCommandRequestDto(reviewing.RoundVersion)
        );
        var resumed = await resumeResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
        Assert.NotNull(resumed);
        Assert.Equal(GameRoundStatusValue.InProgress, resumed.Status);
        Assert.Equal(5, resumed.RoundVersion);
        Assert.Equal(inProgress.GameplayStartedAtUtc, resumed.GameplayStartedAtUtc);
        Assert.Null(resumed.ReviewedAtUtc);
    }

    [Fact]
    public async Task Rebuild_WhenPreparing_RefundsEveryPurchaseOnceAndReopensOrdering()
    {
        var seeded = await SeedActiveGameAsync();
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var prepareResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/prepare",
            new GameRoundVersionCommandRequestDto(1)
        );
        var prepared = await prepareResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(prepared);

        var rebuildResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/rebuild",
            new GameRoundVersionCommandRequestDto(prepared.RoundVersion)
        );
        var rebuilt = await rebuildResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.True(
            rebuildResponse.StatusCode == HttpStatusCode.OK,
            await rebuildResponse.Content.ReadAsStringAsync()
        );
        Assert.NotNull(rebuilt);
        Assert.Equal(GameRoundStatusValue.AwaitingModifiers, rebuilt.Status);
        Assert.Equal(prepared.RoundVersion + 1, rebuilt.RoundVersion);
        Assert.Null(rebuilt.PreparedAtUtc);

        var repeatedResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/rebuild",
            new GameRoundVersionCommandRequestDto(prepared.RoundVersion)
        );
        var repeated = await repeatedResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
        Assert.NotNull(repeated);
        Assert.Equal(rebuilt.RoundVersion, repeated.RoundVersion);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activation = await dbContext.GameModifierActivations.SingleAsync(
            x => x.RoundId == roundId
        );
        Assert.Equal(GameModifierActivationStatusValue.Cancelled, activation.Status);
        Assert.Equal(activation.ActivationCostSnapshot, activation.RefundAmount);
        Assert.Equal(seeded.ModeratorId, activation.CancelledByUserId);
        Assert.Equal("round_rebuild", activation.CancellationReason);

        var audits = await dbContext.GameRoundTransitionAudits
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.Sequence)
            .ToArrayAsync();
        Assert.Collection(
            audits,
            audit => Assert.Equal(GameRoundTransitionActionValue.Prepare, audit.ActionCode),
            audit => Assert.Equal(GameRoundTransitionActionValue.Rebuild, audit.ActionCode)
        );
    }

    [Theory]
    [InlineData(GameRoundStatusValue.AwaitingModifiers)]
    [InlineData(GameRoundStatusValue.Preparing)]
    [InlineData(GameRoundStatusValue.InProgress)]
    [InlineData(GameRoundStatusValue.ReviewingResults)]
    public async Task TechnicalCancel_FromEveryNonterminalStage_RefundsOnceRetiresCardAndFreesTeam(
        string targetStatus
    )
    {
        var seeded = await SeedActiveGameAsync();
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var currentVersion = 1;
        if (targetStatus != GameRoundStatusValue.AwaitingModifiers)
        {
            var prepareResponse = await client.PostAsJsonAsync(
                $"/api/game/rounds/{roundId}/prepare",
                new GameRoundVersionCommandRequestDto(currentVersion)
            );
            var prepared = await prepareResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
            Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
            Assert.NotNull(prepared);
            currentVersion = prepared.RoundVersion;
        }

        if (targetStatus is GameRoundStatusValue.InProgress or GameRoundStatusValue.ReviewingResults)
        {
            var beginResponse = await client.PostAsJsonAsync(
                $"/api/game/rounds/{roundId}/begin-gameplay",
                new GameRoundVersionCommandRequestDto(currentVersion)
            );
            var started = await beginResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
            Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);
            Assert.NotNull(started);
            currentVersion = started.RoundVersion;
        }

        if (targetStatus == GameRoundStatusValue.ReviewingResults)
        {
            var reviewResponse = await client.PostAsJsonAsync(
                $"/api/game/rounds/{roundId}/review",
                new GameRoundVersionCommandRequestDto(currentVersion)
            );
            var reviewing = await reviewResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
            Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
            Assert.NotNull(reviewing);
            currentVersion = reviewing.RoundVersion;
        }

        var request = new TechnicalCancelGameRoundRequestDto(
            currentVersion,
            GameRoundTechnicalCancellationReasonValue.StreamOrInfrastructureFailure,
            null,
            "The external game server became unavailable."
        );
        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/technical-cancel",
            request
        );
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.True(
            cancelResponse.StatusCode == HttpStatusCode.OK,
            await cancelResponse.Content.ReadAsStringAsync()
        );
        Assert.NotNull(cancelled);
        Assert.Equal(GameRoundStatusValue.Cancelled, cancelled.Status);
        Assert.Equal(0, cancelled.FinalScore);
        Assert.Equal(request.ReasonCode, cancelled.TechnicalCancellationReasonCode);
        Assert.Null(cancelled.PublicCancellationSummary);
        Assert.All(cancelled.ModifierResults, x =>
            Assert.Equal(GameRoundModifierOutcomeValue.Cancelled, x.OutcomeStatus));

        var repeatedResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/technical-cancel",
            request
        );
        var repeated = await repeatedResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
        Assert.NotNull(repeated);
        Assert.Equal(cancelled.RoundVersion, repeated.RoundVersion);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activation = await dbContext.GameModifierActivations.SingleAsync(
            x => x.RoundId == roundId
        );
        Assert.Equal(GameModifierActivationStatusValue.Cancelled, activation.Status);
        Assert.Equal(activation.ActivationCostSnapshot, activation.RefundAmount);
        Assert.Equal(BoardCellState.Cancelled, await dbContext.BoardCells
            .Where(x => x.Id == seeded.CellId)
            .Select(x => x.State)
            .SingleAsync());
        Assert.Null(await dbContext.Games
            .Where(x => x.Id == seeded.GameId)
            .Select(x => x.ActiveTeamId)
            .SingleAsync());
        var audits = await dbContext.GameRoundTransitionAudits
            .Where(x => x.RoundId == roundId)
            .OrderBy(x => x.Sequence)
            .ToArrayAsync();
        Assert.Equal(targetStatus, audits[^1].FromStatus);
        Assert.Equal(GameRoundTransitionActionValue.TechnicalCancel, audits[^1].ActionCode);
        Assert.Equal(
            targetStatus switch
            {
                GameRoundStatusValue.AwaitingModifiers => 1,
                GameRoundStatusValue.Preparing => 2,
                GameRoundStatusValue.InProgress => 3,
                GameRoundStatusValue.ReviewingResults => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null)
            },
            audits.Length
        );
    }

    [Fact]
    public async Task BeginGameplay_WhenRoundIsAwaitingModifiers_ReturnsLifecycleConflict()
    {
        var seeded = await SeedActiveGameAsync();
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/begin-gameplay",
            new GameRoundVersionCommandRequestDto(1)
        );
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundNotInProgress, error.Code);
    }

    [Fact]
    public async Task Prepare_WhenViewer_ReturnsForbiddenWithoutChangingRound()
    {
        var seeded = await SeedActiveGameAsync();
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/prepare",
            new GameRoundVersionCommandRequestDto(1)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.SingleAsync(x => x.Id == roundId);
        Assert.Equal(GameRoundStatusValue.AwaitingModifiers, round.Status);
        Assert.Equal(1, round.Version);
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
    public async Task Finalize_WhenModifierResultIdIsRepeated_ReturnsBadRequest()
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
        var modifierResultId = started.ModifierResults[0].ModifierResultId;

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                1,
                0,
                null,
                [
                    new FinalizeGameRoundModifierRequestDto(
                        modifierResultId,
                        null,
                        true
                    ),
                    new FinalizeGameRoundModifierRequestDto(
                        modifierResultId,
                        null,
                        true
                    )
                ]
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.ModifierResolutionDuplicateResult, payload.Code);
    }

    [Fact]
    public async Task PreviewScore_WhenModifierResultIdIsRepeated_ReturnsBadRequest()
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
        var modifierResultId = started.ModifierResults[0].ModifierResultId;

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                1,
                0,
                null,
                [
                    new FinalizeGameRoundModifierRequestDto(
                        modifierResultId,
                        null,
                        true
                    ),
                    new FinalizeGameRoundModifierRequestDto(
                        modifierResultId,
                        null,
                        true
                    )
                ]
            )
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.ModifierResolutionDuplicateResult, payload.Code);
    }

    [Fact]
    public async Task PreviewAndFinalize_WhenStackingBonusIsConfigured_UseSameServerFormula()
    {
        var seeded = await SeedActiveGameAsync();
        await ConfigureSeededModifierAsync(
            seeded,
            "Thirst",
            GameModifierCategories.Result,
            BuiltInModifierBehaviorCatalog.Zhazhda,
            cellCost: 100
        );
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var request = new FinalizeGameRoundRequestDto(
            GameRoundStatusValue.Completed,
            3,
            1,
            null,
            []
        );
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        using (var anonymousClient = _factory.CreateClient())
        {
            var anonymousResponse = await anonymousClient.PostAsJsonAsync(
                $"/api/game/rounds/{started.RoundId}/score-preview",
                request
            );
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        }

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            request
        );
        var preview = await previewResponse.Content.ReadFromJsonAsync<GameRoundScorePreviewDto>();

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        Assert.NotNull(preview);
        Assert.Equal(300, preview.ScoreDetails.KillsScore);
        Assert.Equal(100, preview.ScoreDetails.BountyScore);
        Assert.Equal(45, preview.ScoreDetails.ModifierScoreDelta);
        Assert.Equal(445, preview.ScoreDetails.FinalScore);
        var zhazhdaLine = Assert.Single(
            preview.ScoreDetails.CalculationLines,
            line => line.FormulaCode == ModifierFormulaCodes.KillValueIncreasePerUnit
        );
        Assert.Equal(45, zhazhdaLine.PointsDelta);
        Assert.Equal(445, zhazhdaLine.RunningTotal);
        Assert.Contains(
            zhazhdaLine.Operands,
            value => value.Code == "bonusPerKill" && value.Value == 15m
        );
        Assert.Contains(
            zhazhdaLine.Operands,
            value => value.Code == "adjustedKillValue" && value.Value == 115m
        );
        Assert.Contains(
            zhazhdaLine.Operands,
            value => value.Code == "adjustedKillsScore" && value.Value == 345m
        );
        Assert.Equal(45, Assert.Single(preview.ModifierResults).ScoreDelta);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                GameRoundStatusValue.ReviewingResults,
                await dbContext.GameRounds.Select(x => x.Status).SingleAsync()
            );
            Assert.Equal(
                GameRoundModifierOutcomeValue.Pending,
                await dbContext.GameRoundModifierResults.Select(x => x.OutcomeStatus).SingleAsync()
            );
        }

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            request
        );
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        Assert.NotNull(finalized);
        Assert.Equivalent(preview.ScoreDetails, finalized.ScoreDetails, strict: true);
        Assert.Equal(445, finalized.FinalScore);

        var history = await client.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{seeded.GameId}"
        );
        Assert.NotNull(history);
        var historyRound = Assert.Single(history.MainGame.Rounds);
        Assert.Equal(445, historyRound.FinalScore);
        Assert.Equivalent(finalized.ScoreDetails, historyRound.ScoreDetails, strict: true);
    }

    [Fact]
    public async Task PreviewAndFinalize_WhenBehaviorV2IsAutomatic_UseSameEngineWithoutManualInput()
    {
        var seeded = await SeedActiveGameAsync();
        await ConfigureSeededModifierAsync(
            seeded,
            "Thirst V2",
            GameModifierCategories.Result,
            BuiltInModifierBehaviorCatalog.Zhazhda,
            cellCost: 100
        );
        await ConfigureSeededBehaviorV2Async(
            BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Zhazhda).Behavior,
            revision: 7
        );
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        var reviewResponse = await ReviewRoundAsync(seeded, started.RoundId);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(reviewed);
        var request = new FinalizeGameRoundRequestDto(
            GameRoundStatusValue.Completed,
            3,
            1,
            null,
            [],
            null,
            reviewed.RoundVersion
        );
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            request
        );
        var preview = await previewResponse.Content.ReadFromJsonAsync<GameRoundScorePreviewDto>();
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        Assert.NotNull(preview);
        Assert.Equal(reviewed.RoundVersion, preview.RoundVersion);
        Assert.Equal(64, preview.NormalizedInputHash.Length);
        Assert.Equal(445, preview.ScoreDetails.FinalScore);
        Assert.Equal(45, Assert.Single(preview.ModifierResults).ScoreDelta);
        var trace = Assert.Single(preview.CalculationTrace);
        Assert.Equal(ModifierFormulaCodes.KillValueIncreasePerUnit, trace.FormulaCode);
        Assert.Equal(45, trace.PointsDelta);
        Assert.Equal(0, trace.BonusKillsDelta);

        var automaticResultId = Assert.Single(started.ModifierResults).ModifierResultId;
        var artificialAutomaticInputResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            request with
            {
                ModifierResults =
                [
                    new FinalizeGameRoundModifierRequestDto(
                        automaticResultId,
                        null,
                        null
                    )
                ]
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, artificialAutomaticInputResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.automatic_input_forbidden",
            (await artificialAutomaticInputResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var stalePreviewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            request with { ExpectedRoundVersion = reviewed.RoundVersion - 1 }
        );
        Assert.Equal(HttpStatusCode.Conflict, stalePreviewResponse.StatusCode);

        using (var viewerClient = TestAuthClientFactory.CreateClient(
                   _factory,
                   [AuthRoleCodes.Viewer],
                   userId: Guid.NewGuid()
               ))
        {
            var viewerRound = await viewerClient.GetFromJsonAsync<GameRoundDetailsDto>(
                "/api/game/rounds/active"
            );
            Assert.NotNull(viewerRound);
            Assert.True(viewerRound.ServerNowUtc <= DateTime.UtcNow.AddSeconds(1));
            var runtimeBehavior = Assert.Single(viewerRound.ModifierResults).RuntimeBehavior;
            Assert.NotNull(runtimeBehavior);
            Assert.False(string.IsNullOrWhiteSpace(runtimeBehavior.Rule));
            var viewerJson = await viewerClient.GetStringAsync("/api/game/rounds/active");
            Assert.DoesNotContain("calculationTrace", viewerJson, StringComparison.Ordinal);
            Assert.DoesNotContain(
                ModifierFormulaCodes.GrowingKillValue,
                viewerJson,
                StringComparison.Ordinal
            );
        }

        using (var previewScope = _factory.Services.CreateScope())
        {
            var previewDb = previewScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = await previewDb.GameRoundModifierResults.SingleAsync();
            Assert.Equal(GameRoundModifierOutcomeValue.Pending, pending.OutcomeStatus);
            Assert.Null(pending.CalculationBreakdownJson);
        }

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            request
        );
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        Assert.NotNull(finalized);
        Assert.Equivalent(preview.ScoreDetails, finalized.ScoreDetails, strict: true);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await dbContext.GameRoundModifierResults.SingleAsync();
        Assert.Equal(7, persisted.DefinitionRevisionSnapshot);
        Assert.NotNull(persisted.CalculationBreakdownJson);
    }

    [Fact]
    public async Task PreviewAndFinalize_WhenBehaviorV2SnapshotIsInvalid_Return422WithoutPartialPersistence()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        Assert.Equal(HttpStatusCode.OK, (await ReviewRoundAsync(seeded, started.RoundId)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var result = await dbContext.GameRoundModifierResults.SingleAsync();
            result.ModifierBehaviorV2SnapshotJson = "{}";
            await dbContext.SaveChangesAsync();
        }

        var request = new FinalizeGameRoundRequestDto(
            GameRoundStatusValue.Completed,
            1,
            0,
            null,
            []
        );
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            request
        );
        Assert.Equal(HttpStatusCode.UnprocessableEntity, previewResponse.StatusCode);
        Assert.Equal(
            "behavior.invalid",
            (await previewResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            request
        );
        Assert.Equal(HttpStatusCode.UnprocessableEntity, finalizeResponse.StatusCode);
        Assert.Equal(
            "behavior.invalid",
            (await finalizeResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await verificationDb.GameRounds.SingleAsync();
        var modifierResult = await verificationDb.GameRoundModifierResults.SingleAsync();
        Assert.Equal(GameRoundStatusValue.ReviewingResults, round.Status);
        Assert.Null(round.FinalScore);
        Assert.Equal(GameRoundModifierOutcomeValue.Pending, modifierResult.OutcomeStatus);
        Assert.Null(modifierResult.CalculationBreakdownJson);
    }

    [Fact]
    public async Task PreviewAndFinalize_WhenBehaviorV2RuleGroup_RequireExactMembersAndViolationComment()
    {
        var seeded = await SeedActiveGameAsync();
        await ConfigureSeededBehaviorV2Async(
            BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior,
            revision: 3
        );
        await SeedAwaitingModifiersRoundAsync(seeded);
        await SeedSecondModifierActivationAsync(seeded);
        var startResponse = await StartRoundAsync(seeded);
        Assert.Equal(HttpStatusCode.Created, startResponse.StatusCode);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        Assert.Equal(2, started.ModifierResults.Count);
        Assert.All(started.ModifierResults, modifier =>
        {
            Assert.NotNull(modifier.ResolutionGroupId);
            Assert.Equal("ruleStatus", modifier.ResolutionKind);
        });
        var resolutionGroupId = Assert.Single(
            started.ModifierResults.Select(x => x.ResolutionGroupId).Distinct()
        );
        var memberIds = started.ModifierResults.Select(x => x.ModifierResultId).ToArray();
        Assert.Equal(HttpStatusCode.OK, (await ReviewRoundAsync(seeded, started.RoundId)).StatusCode);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        FinalizeGameRoundRequestDto Request(string? comment, IReadOnlyList<string> memberIds) =>
            new(
                GameRoundStatusValue.Completed,
                1,
                0,
                null,
                [],
                [
                    new FinalizeGameRoundRuleGroupRequestDto(
                        resolutionGroupId!,
                        memberIds,
                        "violated",
                        comment
                    )
                ]
            );

        var missingCommentResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            Request(null, memberIds)
        );
        var alteredMembersResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            Request("Нарушение подтверждено", [Guid.NewGuid().ToString()])
        );
        Assert.Equal(HttpStatusCode.BadRequest, missingCommentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, alteredMembersResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.violation_comment_required",
            (await missingCommentResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );
        Assert.Equal(
            "modifier_resolution.group_members_mismatch",
            (await alteredMembersResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var validRequest = Request("  Нарушение подтверждено  ", memberIds);
        var missingGroupResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            validRequest with { RuleGroups = [] }
        );
        var validGroup = Assert.Single(validRequest.RuleGroups!);
        var duplicateGroupResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            validRequest with { RuleGroups = [validGroup, validGroup] }
        );
        Assert.Equal(HttpStatusCode.BadRequest, missingGroupResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateGroupResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.group_set_mismatch",
            (await missingGroupResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );
        Assert.Equal(
            "modifier_resolution.duplicate_group",
            (await duplicateGroupResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            validRequest
        );
        var preview = await previewResponse.Content.ReadFromJsonAsync<GameRoundScorePreviewDto>();
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        Assert.NotNull(preview);
        Assert.Equal(2, preview.ModifierResults.Count);
        Assert.All(preview.ModifierResults, previewModifier =>
        {
            Assert.Equal(GameRoundModifierOutcomeValue.Violated, previewModifier.OutcomeStatus);
            Assert.Equal("Нарушение подтверждено", previewModifier.ViolationComment);
            Assert.Equal(0, previewModifier.ScoreDelta);
        });
        Assert.Equal(started.BaseScore, preview.ScoreDetails.FinalScore);

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            validRequest
        );
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        Assert.NotNull(finalized);
        Assert.Equal(2, finalized.ModifierResults.Count);
        Assert.All(finalized.ModifierResults, modifier =>
            Assert.Equal("Нарушение подтверждено", modifier.ViolationComment));

        var history = await client.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{seeded.GameId}"
        );
        Assert.NotNull(history);
        var historyModifiers = Assert.Single(history.MainGame.Rounds).Modifiers;
        Assert.Equal(2, historyModifiers.Count);
        Assert.All(historyModifiers, historyModifier =>
        {
            Assert.Equal("Нарушение подтверждено", historyModifier.ViolationComment);
            Assert.Equal(3, historyModifier.DefinitionRevision);
        });

        using var viewerClient = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: Guid.NewGuid()
        );
        var viewerHistory = await viewerClient.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{seeded.GameId}"
        );
        Assert.NotNull(viewerHistory);
        Assert.All(Assert.Single(viewerHistory.MainGame.Rounds).Modifiers, modifier =>
            Assert.Equal("Нарушение подтверждено", modifier.ViolationComment));
    }

    [Fact]
    public async Task PreviewAndFinalize_WhenV2BonusKillAndWindowBonusInteract_ResolveBonusKillsFirst()
    {
        var seeded = await SeedActiveGameAsync();
        await ConfigureSeededBehaviorV2Async(
            BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Patron).Behavior
        );
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        await AddBehaviorV2ActivationAsync(
            seeded,
            roundId,
            "Hard75 V2",
            BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Hard75).Behavior
        );
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        Assert.Equal(2, started.ModifierResults.Count);
        var booleanResult = Assert.Single(
            started.ModifierResults,
            result => result.ResolutionKind == "boolean"
        );
        var countResult = Assert.Single(
            started.ModifierResults,
            result => result.ResolutionKind == "nonNegativeCount"
        );
        Assert.Equal(HttpStatusCode.OK, (await ReviewRoundAsync(seeded, started.RoundId)).StatusCode);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        FinalizeGameRoundRequestDto Request(int windowKills) => new(
            GameRoundStatusValue.Completed,
            1,
            0,
            null,
            [
                new FinalizeGameRoundModifierRequestDto(
                    booleanResult.ModifierResultId,
                    null,
                    true
                ),
                new FinalizeGameRoundModifierRequestDto(
                    countResult.ModifierResultId,
                    windowKills,
                    null
                )
            ]
        );

        var invalidResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            Request(3)
        );
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal(
            "resolution.count_exceeds_resolved_kills",
            (await invalidResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var negativeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            Request(-1)
        );
        Assert.Equal(HttpStatusCode.BadRequest, negativeResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.non_negative_count_required",
            (await negativeResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var zeroResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            Request(0)
        );
        Assert.Equal(HttpStatusCode.OK, zeroResponse.StatusCode);

        var oneCountRequest = Request(1);
        var oneCountResults = Assert.IsAssignableFrom<
            IReadOnlyList<FinalizeGameRoundModifierRequestDto>
        >(oneCountRequest.ModifierResults);
        var duplicateResult = Assert.Single(oneCountResults, item => item.CountValue == 1);
        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            oneCountRequest with { ModifierResults = [duplicateResult, duplicateResult] }
        );
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.duplicate_result",
            (await duplicateResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var extraResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            oneCountRequest with
            {
                ModifierResults =
                [
                    .. oneCountResults,
                    duplicateResult with { ModifierResultId = Guid.NewGuid().ToString() }
                ]
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, extraResponse.StatusCode);
        Assert.Equal(
            "modifier_resolution.result_set_mismatch",
            (await extraResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var validRequest = Request(2);
        var previewResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/score-preview",
            validRequest
        );
        var preview = await previewResponse.Content.ReadFromJsonAsync<GameRoundScorePreviewDto>();
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        Assert.NotNull(preview);
        Assert.Equal(1, preview.ScoreDetails.ModifierKillDelta);
        Assert.Equal(started.BaseScore, preview.ScoreDetails.ModifierKillScore);
        Assert.Equal(
            (int)(2 * started.BaseScore * 0.75m),
            preview.ScoreDetails.ModifierScoreDelta
        );
        Assert.Equal(
            started.BaseScore * 2 + (int)(2 * started.BaseScore * 0.75m),
            preview.ScoreDetails.FinalScore
        );

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            validRequest
        );
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        Assert.NotNull(finalized);
        Assert.Equivalent(preview.ScoreDetails, finalized.ScoreDetails, strict: true);
    }

    [Fact]
    public async Task Finalize_WhenMentorKillsAreReported_ConvertsThemToScoredKills()
    {
        var seeded = await SeedActiveGameAsync();
        await ConfigureSeededModifierAsync(
            seeded,
            "Rat",
            GameModifierCategories.Result,
            BuiltInModifierBehaviorCatalog.Krysa,
            cellCost: 100
        );
        var startResponse = await StartRoundAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(started);
        Assert.Equal(HttpStatusCode.OK, (await ReviewRoundAsync(seeded, started.RoundId)).StatusCode);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/rounds/{started.RoundId}/finalize",
            new FinalizeGameRoundRequestDto(
                GameRoundStatusValue.Completed,
                1,
                0,
                null,
                [
                    new FinalizeGameRoundModifierRequestDto(
                        started.ModifierResults[0].ModifierResultId,
                        2,
                        null
                    )
                ]
            )
        );
        var payload = await response.Content.ReadFromJsonAsync<GameRoundDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(2, payload.ScoreDetails.ModifierKillDelta);
        Assert.Equal(200, payload.ScoreDetails.ModifierKillScore);
        Assert.Equal(3, payload.ScoreDetails.TotalKillCount);
        Assert.Equal(300, payload.FinalScore);
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

        var activeRound = await client.GetFromJsonAsync<GameRoundDetailsDto>(
            "/api/game/rounds/active"
        );
        Assert.NotNull(activeRound);

        return await client.PostAsJsonAsync(
            $"/api/game/rounds/{roundId}/review",
            new GameRoundVersionCommandRequestDto(activeRound.RoundVersion)
        );
    }

    private async Task SeedSecondStackedCustomAutoScoreModifierAsync(SeededActiveGame seeded)
    {
        using (var definitionScope = _factory.Services.CreateScope())
        {
            var definitionDb = definitionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var definition = await definitionDb.ModifierDefinitions.SingleAsync();
            definition.Category = GameModifierCategories.Result;
            definition.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Zhazhda).Behavior
            );
            definition.UpdatedAtUtc = DateTime.UtcNow;
            await definitionDb.SaveChangesAsync();
        }

        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var activationScope = _factory.Services.CreateScope();
        var activationDb = activationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await activationDb.ModifierDefinitions.SingleAsync();
        activationDb.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                RoundId = roundId,
                ModifierId = modifier.Id,
                ActivatedByUserId = seeded.ModeratorId,
                InitiatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = modifier.ActivationCost,
                DefinitionRevisionSnapshot = modifier.Revision,
                ModifierNameSnapshot = modifier.Name,
                ModifierDescriptionSnapshot = modifier.Description,
                ModifierCategorySnapshot = modifier.Category,
                ModifierIconEmojiSnapshot = modifier.IconEmoji,
                ActivationCommandSnapshot = modifier.ActivationCommand,
                NormalizedTagsSnapshot = modifier.NormalizedTags.ToArray(),
                BehaviorV2SnapshotJson = modifier.BehaviorV2Json,
                ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-9)
            }
        );
        await activationDb.SaveChangesAsync();
    }

    private async Task ConfigureSeededBehaviorV2Async(
        ModifierBehaviorV2 behavior,
        int revision = 1
    )
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await dbContext.ModifierDefinitions.SingleAsync();
        modifier.Revision = revision;
        modifier.NormalizedTags = ["behavior-v2"];
        modifier.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(behavior);
        await dbContext.SaveChangesAsync();
    }

    private async Task AddBehaviorV2ActivationAsync(
        SeededActiveGame seeded,
        Guid roundId,
        string name,
        ModifierBehaviorV2 behavior
    )
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifierId = Guid.NewGuid();
        var behaviorJson = ModifierBehaviorV2Json.Serialize(behavior);
        var now = DateTime.UtcNow;
        dbContext.ModifierDefinitions.Add(
            new ModifierDefinition
            {
                Id = modifierId,
                Revision = 1,
                Name = name,
                Description = name,
                Category = GameModifierCategories.Result,
                ActivationCost = 1,
                NormalizedTags = ["behavior-v2"],
                BehaviorV2Json = behaviorJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                RoundId = roundId,
                ModifierId = modifierId,
                ActivatedByUserId = seeded.ModeratorId,
                InitiatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = 1,
                DefinitionRevisionSnapshot = 1,
                ModifierNameSnapshot = name,
                ModifierDescriptionSnapshot = name,
                ModifierCategorySnapshot = GameModifierCategories.Result,
                NormalizedTagsSnapshot = ["behavior-v2"],
                BehaviorV2SnapshotJson = behaviorJson,
                ActivatedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedSecondModifierActivationAsync(SeededActiveGame seeded)
    {
        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await dbContext.ModifierDefinitions.SingleAsync();

        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                RoundId = roundId,
                ModifierId = modifier.Id,
                ActivatedByUserId = seeded.ModeratorId,
                InitiatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = modifier.ActivationCost,
                DefinitionRevisionSnapshot = modifier.Revision,
                ModifierNameSnapshot = modifier.Name,
                ModifierDescriptionSnapshot = modifier.Description,
                ModifierCategorySnapshot = modifier.Category,
                ModifierIconEmojiSnapshot = modifier.IconEmoji,
                ActivationCommandSnapshot = modifier.ActivationCommand,
                NormalizedTagsSnapshot = modifier.NormalizedTags.ToArray(),
                BehaviorV2SnapshotJson = modifier.BehaviorV2Json,
                ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-9)
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task ConfigureSeededModifierAsync(
        SeededActiveGame seeded,
        string name,
        string category,
        string behaviorCode,
        int cellCost
    )
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await dbContext.ModifierDefinitions.SingleAsync();
        modifier.Name = name;
        modifier.Category = category;
        modifier.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(
            BuiltInModifierBehaviorCatalog.Get(behaviorCode).Behavior
        );
        modifier.UpdatedAtUtc = DateTime.UtcNow;
        await SetSeededCellCostAsync(dbContext, seeded, cellCost);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedSecondAutomaticFailurePenaltyModifierAsync(SeededActiveGame seeded)
    {
        await SetSeededCellCostAsync(seeded, 100);
        using (var definitionScope = _factory.Services.CreateScope())
        {
            var definitionDb = definitionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var definition = await definitionDb.ModifierDefinitions.SingleAsync();
            definition.Name = "Thirst";
            definition.Category = GameModifierCategories.Result;
            definition.BehaviorV2Json = ModifierBehaviorV2Json.Serialize(
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Zhazhda).Behavior
            );
            definition.UpdatedAtUtc = DateTime.UtcNow;
            await definitionDb.SaveChangesAsync();
        }

        var roundId = await SeedAwaitingModifiersRoundAsync(seeded);
        using var activationScope = _factory.Services.CreateScope();
        var activationDb = activationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var modifier = await activationDb.ModifierDefinitions.SingleAsync();
        activationDb.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                RoundId = roundId,
                ModifierId = modifier.Id,
                ActivatedByUserId = seeded.ModeratorId,
                InitiatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = modifier.ActivationCost,
                ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-9)
            }
        );
        await activationDb.SaveChangesAsync();
    }

    private async Task SetSeededCellCostAsync(SeededActiveGame seeded, int cost)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SetSeededCellCostAsync(dbContext, seeded, cost);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SetSeededCellCostAsync(
        ApplicationDbContext dbContext,
        SeededActiveGame seeded,
        int cost
    )
    {
        var cell = await dbContext.BoardCells.SingleAsync(x => x.Id == seeded.CellId);
        cell.Cost = cost;
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
                SlotType = "open",
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
                Category = "round",
                ActivationCost = 5,
                Revision = 1,
                NormalizedTags = ["test"],
                BehaviorV2Json = ModifierBehaviorV2Json.Serialize(
                    BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Zhazhda).Behavior
                ),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
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
        var existingRoundId = await dbContext.GameRounds
            .Where(
                x =>
                    x.GameId == seeded.GameId
                    && x.Status == GameRoundStatusValue.AwaitingModifiers
            )
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();
        if (existingRoundId.HasValue)
        {
            return existingRoundId.Value;
        }

        var now = DateTime.UtcNow.AddMinutes(-5);
        var roundId = Guid.NewGuid();
        var cellSnapshot = await dbContext.BoardCells
            .AsNoTracking()
            .Where(x => x.Id == seeded.CellId)
            .Select(x => new { x.Cost, x.Title })
            .SingleAsync();

        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = seeded.GameId,
                BoardCellId = seeded.CellId,
                TeamId = seeded.TeamId,
                Status = GameRoundStatusValue.AwaitingModifiers,
                StartedAtUtc = now,
                BaseScore = cellSnapshot.Cost,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = cellSnapshot.Title,
                CellCostSnapshot = cellSnapshot.Cost,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        var modifier = await dbContext.ModifierDefinitions.AsNoTracking().SingleAsync();
        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                RoundId = roundId,
                ModifierId = modifier.Id,
                ActivatedByUserId = seeded.ModeratorId,
                InitiatedByUserId = seeded.ModeratorId,
                ActivationCostSnapshot = modifier.ActivationCost,
                DefinitionRevisionSnapshot = modifier.Revision,
                ModifierNameSnapshot = modifier.Name,
                ModifierDescriptionSnapshot = modifier.Description,
                ModifierCategorySnapshot = modifier.Category,
                ModifierIconEmojiSnapshot = modifier.IconEmoji,
                ActivationCommandSnapshot = modifier.ActivationCommand,
                NormalizedTagsSnapshot = modifier.NormalizedTags.ToArray(),
                BehaviorV2SnapshotJson = modifier.BehaviorV2Json,
                ActivatedAtUtc = now
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
