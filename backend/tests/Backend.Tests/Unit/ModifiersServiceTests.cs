using backend.Application.Abstractions.Repositories;
using backend.Application.Features.Modifiers;
using DomainActiveModifier = backend.Domain.Models.ActiveModifier;
using DomainModifierDefinition = backend.Domain.Models.ModifierDefinition;

namespace Backend.Tests.Unit;

public sealed class ModifiersServiceTests
{
    [Fact]
    public async Task ActivateAsync_WhenDefinitionMissing_DoesNotChangeActiveList()
    {
        var repository = new FakeModifiersRepository(
            definitions:
            [
                new DomainModifierDefinition { Id = "m1", Name = "One", Cost = 1, Description = "d" }
            ],
            active: []
        );
        var service = new ModifiersService(repository);

        var snapshot = await service.ActivateAsync(
            new backend.Application.Contracts.ActivateModifierCommand("unknown", "viewer"),
            CancellationToken.None
        );

        Assert.Empty(snapshot.Active);
        Assert.Equal(0, repository.SaveCallCount);
    }

    [Fact]
    public async Task ActivateAsync_PrependsNewActiveModifier()
    {
        var repository = new FakeModifiersRepository(
            definitions: [new DomainModifierDefinition { Id = "m1", Name = "One", Cost = 1, Description = "d" }],
            active:
            [
                new DomainActiveModifier
                {
                    Id = "old",
                    ModifierId = "m1",
                    ActivatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    TriggeredBy = "a"
                }
            ]
        );
        var service = new ModifiersService(repository);

        var snapshot = await service.ActivateAsync(
            new backend.Application.Contracts.ActivateModifierCommand("m1", "viewer"),
            CancellationToken.None
        );

        Assert.Equal(2, snapshot.Active.Count);
        Assert.Equal("m1", snapshot.Active[0].ModifierId);
        Assert.Equal("viewer", snapshot.Active[0].TriggeredBy);
        Assert.Equal("m1", snapshot.Active[1].ModifierId);
        Assert.True(repository.SaveCallCount >= 1);
    }

    [Fact]
    public async Task ActivateAsync_TrimsToMaxTenActive_RemovesOldestEntries()
    {
        var old = DateTimeOffset.Parse("2020-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var active = Enumerable
            .Range(0, 10)
            .Select(
                i => new DomainActiveModifier
                {
                    Id = $"a{i}",
                    ModifierId = "m1",
                    ActivatedAt = old.AddMinutes(i),
                    TriggeredBy = "x"
                }
            )
            .ToList();

        var repository = new FakeModifiersRepository(
            definitions: [new DomainModifierDefinition { Id = "m1", Name = "One", Cost = 1, Description = "d" }],
            active
        );
        var service = new ModifiersService(repository);

        var snapshot = await service.ActivateAsync(
            new backend.Application.Contracts.ActivateModifierCommand("m1", "viewer"),
            CancellationToken.None
        );

        Assert.Equal(10, snapshot.Active.Count);
        Assert.DoesNotContain(snapshot.Active, modifier => modifier.Id == "a9");
        Assert.Equal("viewer", snapshot.Active[0].TriggeredBy);
    }

    private sealed class FakeModifiersRepository : IModifiersRepository
    {
        private readonly List<DomainModifierDefinition> _definitions;
        private List<DomainActiveModifier> _active;

        public int SaveCallCount { get; private set; }

        public FakeModifiersRepository(
            IReadOnlyList<DomainModifierDefinition> definitions,
            IReadOnlyList<DomainActiveModifier> active
        )
        {
            _definitions = definitions.ToList();
            _active = active.ToList();
        }

        public Task<IReadOnlyList<DomainModifierDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<IReadOnlyList<DomainModifierDefinition>>(_definitions);
        }

        public Task<IReadOnlyList<DomainActiveModifier>> GetActiveModifiersAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<IReadOnlyList<DomainActiveModifier>>(_active);
        }

        public Task SaveActiveModifiersAsync(
            IReadOnlyList<DomainActiveModifier> activeModifiers,
            CancellationToken cancellationToken = default
        )
        {
            SaveCallCount++;
            _active = activeModifiers.ToList();
            return Task.CompletedTask;
        }
    }
}
