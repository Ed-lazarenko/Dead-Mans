using backend.Application.Contracts;
using backend.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Tests.Unit.Infrastructure.Realtime;

public sealed class SignalRGameBoardEventsPublisherTests
{
    [Fact]
    public async Task PublishCellOpenedAsync_SendsCellOpenedEventToAllClients()
    {
        var proxy = new CapturingClientProxy();
        var hubContext = new FakeHubContext(new FakeHubClients(proxy));
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

        Assert.Equal(SignalRGameBoardEventsPublisher.CellOpenedEventName, proxy.Method);
        Assert.NotNull(proxy.Args);
        Assert.Single(proxy.Args!);
        var sentPayload = Assert.IsType<GameCellOpenedEvent>(proxy.Args![0]);
        Assert.Equal(payload, sentPayload);
    }

    private sealed class FakeHubContext : IHubContext<GameBoardHub>
    {
        public FakeHubContext(IHubClients clients)
        {
            Clients = clients;
            Groups = new FakeGroupManager();
        }

        public IHubClients Clients { get; }
        public IGroupManager Groups { get; }
    }

    private sealed class FakeHubClients : IHubClients
    {
        private readonly IClientProxy _proxy;

        public FakeHubClients(IClientProxy proxy)
        {
            _proxy = proxy;
        }

        public IClientProxy All => _proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Client(string connectionId) => _proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public IClientProxy Group(string groupName) => _proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
        public IClientProxy User(string userId) => _proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
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
}
