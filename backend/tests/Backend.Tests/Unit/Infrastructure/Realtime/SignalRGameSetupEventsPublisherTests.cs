using backend.Api.Realtime;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Backend.Tests.Unit.Infrastructure.Realtime;

public sealed class SignalRGameSetupEventsPublisherTests
{
    [Fact]
    public async Task PublishDraftChangedAsync_SendsDraftChangedEventToRealtimeGroup()
    {
        var clients = new FakeHubClients();
        var hubContext = new FakeHubContext(clients);
        var publisher = new SignalRGameSetupEventsPublisher(hubContext);

        await publisher.PublishDraftChangedAsync();

        Assert.Equal(RealtimeGroupNames.GameSetupAudience, clients.LastGroupName);
        var proxy = clients.GroupProxy;
        Assert.Equal(SignalRGameSetupEventsPublisher.DraftChangedEventName, proxy.Method);
        Assert.Empty(proxy.Args ?? []);
    }

    [Fact]
    public async Task OnConnectedAsync_AddsConnectionToRealtimeGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = new GameSetupHub(new LoggerFactory().CreateLogger<GameSetupHub>())
        {
            Context = new FakeHubCallerContext("connection-2"),
            Groups = groups,
        };

        await hub.OnConnectedAsync();

        Assert.Equal("connection-2", groups.LastConnectionId);
        Assert.Equal(RealtimeGroupNames.GameSetupAudience, groups.LastGroupName);
    }

    private sealed class FakeHubContext : IHubContext<GameSetupHub>
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
        public RecordingClientProxy GroupProxy { get; } = new();
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

    private sealed class RecordingClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Args { get; private set; }

        public Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = methodName;
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
