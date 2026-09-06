using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.Auth;

public sealed class RealtimeConnectionContractTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly TestWebApplicationFactory _factory;

    public RealtimeConnectionContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GameBoardHub_AuthenticatedViewer_ConnectsAndReceivesPublishedEvent()
    {
        using var authenticatedFactory = TestAuthClientFactory.CreateFactory(
            _factory,
            [AuthRoleCodes.Viewer]
        );
        await using var connection = CreateConnection(
            authenticatedFactory,
            RealtimeHubContracts.GameBoard.HubPath
        );
        var receivedEvent = new TaskCompletionSource<GameRoundStateChangedEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var subscription = connection.On<GameRoundStateChangedEventDto>(
            RealtimeHubContracts.GameBoard.RoundStateChangedEvent,
            payload => receivedEvent.TrySetResult(payload)
        );

        await connection.StartAsync();

        var gameId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;
        var publisher = authenticatedFactory.Services.GetRequiredService<IGameBoardEventsPublisher>();
        await publisher.PublishRoundStateChangedAsync(
            new GameRoundStateChangedEvent(
                gameId,
                roundId,
                "gameplay",
                7,
                occurredAtUtc
            )
        );

        var payload = await receivedEvent.Task.WaitAsync(EventTimeout);

        Assert.Equal(gameId.ToString(), payload.GameId);
        Assert.Equal(roundId.ToString(), payload.RoundId);
        Assert.Equal("gameplay", payload.Status);
        Assert.Equal(7, payload.RoundVersion);
        Assert.Equal(occurredAtUtc, payload.OccurredAtUtc);
    }

    [Fact]
    public async Task GameSetupHub_Admin_ConnectsSuccessfully()
    {
        using var authenticatedFactory = TestAuthClientFactory.CreateFactory(
            _factory,
            [AuthRoleCodes.Admin]
        );
        await using var connection = CreateConnection(
            authenticatedFactory,
            RealtimeHubContracts.GameSetup.HubPath
        );

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    private static HubConnection CreateConnection(
        WebApplicationFactory<Program> factory,
        string hubPath
    )
    {
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(factory.Server.BaseAddress, hubPath),
                options =>
                {
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                }
            )
            .Build();
    }
}
