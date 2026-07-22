using backend.Api.Contracts;
using backend.Application.Contracts;
using backend.Domain.Persistence;
using backend.Infrastructure.Realtime;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Backend.Tests.Unit.Infrastructure.Realtime;

public sealed class SignalRGameBoardEventsPublisherTests
{
    [Fact]
    public async Task PublishCellOpenedAsync_SendsCellOpenedEventToRealtimeGroup()
    {
        var clients = new FakeHubClients();
        var hubContext = new FakeHubContext(clients);
        var publisher = new SignalRGameBoardEventsPublisher(hubContext);
        var payload = new GameCellOpenedEvent(
            GameId: Guid.NewGuid().ToString(),
            Version: 5,
            Cell: new GameBoardCell(
                Id: Guid.NewGuid().ToString(),
                Row: 0,
                Col: 1,
                CellType: "tile",
                Title: "Cell",
                Description: null,
                Cost: 100,
                State: backend.Domain.Models.GameBoardCellState.Open,
                Media: []
            )
        );

        await publisher.PublishCellOpenedAsync(payload);

        Assert.Equal(RealtimeGroupNames.GameBoardAudience, clients.LastGroupName);
        var proxy = clients.GroupProxy;
        Assert.Equal(SignalRGameBoardEventsPublisher.CellOpenedEventName, proxy.Method);
        Assert.NotNull(proxy.Args);
        Assert.Single(proxy.Args!);
        var sentPayload = Assert.IsType<GameCellOpenedEventDto>(proxy.Args![0]);
        Assert.Equal(payload.GameId, sentPayload.GameId);
        Assert.Equal(payload.Version, sentPayload.Version);
        Assert.Equal(payload.Cell.Id, sentPayload.Cell.Id);
        Assert.Equal(payload.Cell.State.ToString().ToLowerInvariant(), sentPayload.Cell.State);
    }

    [Fact]
    public async Task PublishQuizStateChangedAsync_SendsQuizStateChangedEventToRealtimeGroup()
    {
        var clients = new FakeHubClients();
        var hubContext = new FakeHubContext(clients);
        var publisher = new SignalRGameBoardEventsPublisher(hubContext);
        var payload = new GameQuizStateChangedEvent(
            Guid.NewGuid(),
            GameQuizStateChangeKinds.QuestionAnswered,
            DateTime.UtcNow
        );

        await publisher.PublishQuizStateChangedAsync(payload);

        Assert.Equal(RealtimeGroupNames.GameBoardAudience, clients.LastGroupName);
        var proxy = clients.GroupProxy;
        Assert.Equal(SignalRGameBoardEventsPublisher.QuizStateChangedEventName, proxy.Method);
        Assert.NotNull(proxy.Args);
        Assert.Single(proxy.Args!);
        var sentPayload = Assert.IsType<GameQuizStateChangedEventDto>(proxy.Args![0]);
        Assert.Equal(payload.GameId.ToString(), sentPayload.GameId);
        Assert.Equal(payload.ChangeKind, sentPayload.ChangeKind);
        Assert.Equal(payload.OccurredAtUtc, sentPayload.OccurredAtUtc);
    }

    [Fact]
    public async Task PublishCardRunStateChangedAsync_SendsRoundStateChangedEventToRealtimeGroup()
    {
        var clients = new FakeHubClients();
        var hubContext = new FakeHubContext(clients);
        var publisher = new SignalRGameBoardEventsPublisher(hubContext);
        var payload = new GameCardRunStateChangedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            GameCardRunStatusValue.AwaitingModifiers,
            DateTime.UtcNow
        );

        await publisher.PublishCardRunStateChangedAsync(payload);

        Assert.Equal(RealtimeGroupNames.GameBoardAudience, clients.LastGroupName);
        var proxy = clients.GroupProxy;
        Assert.Equal(SignalRGameBoardEventsPublisher.CardRunStateChangedEventName, proxy.Method);
        Assert.NotNull(proxy.Args);
        Assert.Single(proxy.Args!);
        var sentPayload = Assert.IsType<GameCardRunStateChangedEventDto>(proxy.Args![0]);
        Assert.Equal(payload.GameId.ToString(), sentPayload.GameId);
        Assert.Equal(payload.CardRunId.ToString(), sentPayload.CardRunId);
        Assert.Equal(payload.Status, sentPayload.Status);
        Assert.Equal(payload.OccurredAtUtc, sentPayload.OccurredAtUtc);
    }

    [Fact]
    public async Task OnConnectedAsync_AddsConnectionToRealtimeGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = new GameBoardHub(new LoggerFactory().CreateLogger<GameBoardHub>())
        {
            Context = new FakeHubCallerContext("connection-1"),
            Groups = groups,
        };

        await hub.OnConnectedAsync();

        Assert.Equal("connection-1", groups.LastConnectionId);
        Assert.Equal(RealtimeGroupNames.GameBoardAudience, groups.LastGroupName);
    }

    private sealed class FakeHubContext : IHubContext<GameBoardHub>
    {
        public FakeHubContext(IHubClients clients)
        {
            Clients = clients;
            Groups = new RecordingGroupManager();
        }

        public IHubClients Clients { get; }
        public IGroupManager Groups { get; }
    }

    private sealed class FakeHubClients : IHubClients
    {
        public CapturingClientProxy GroupProxy { get; } = new();
        public string? LastGroupName { get; private set; }

        public IClientProxy All => GroupProxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => GroupProxy;
        public IClientProxy Client(string connectionId) => GroupProxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => GroupProxy;
        public IClientProxy Group(string groupName)
        {
            LastGroupName = groupName;
            return GroupProxy;
        }
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Group(groupName);
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => Group(groupNames[0]);
        public IClientProxy User(string userId) => GroupProxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => GroupProxy;
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public string? LastConnectionId { get; private set; }
        public string? LastGroupName { get; private set; }

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            LastConnectionId = connectionId;
            LastGroupName = groupName;
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Args { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = method;
            Args = args;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;

        public FakeHubCallerContext(string connectionId)
        {
            _connectionId = connectionId;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features => new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }
}
