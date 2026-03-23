using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.Modifiers;

public sealed class ModifiersService : IModifiersService
{
    private const int MaxActiveModifiers = 10;

    private readonly IModifiersRepository _repository;

    public ModifiersService(IModifiersRepository repository)
    {
        _repository = repository;
    }

    public async Task<ModifiersSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _repository.GetDefinitionsAsync(cancellationToken);
        var activeModifiers = await _repository.GetActiveModifiersAsync(cancellationToken);

        return BuildSnapshot(definitions, activeModifiers);
    }

    public async Task<ModifiersSnapshot> ActivateAsync(
        ActivateModifierCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var definitions = await _repository.GetDefinitionsAsync(cancellationToken);
        var activeModifiers = (await _repository.GetActiveModifiersAsync(cancellationToken)).ToList();

        var definitionExists = definitions.Any(definition => definition.Id == command.ModifierId);
        if (!definitionExists)
        {
            return BuildSnapshot(definitions, activeModifiers);
        }

        activeModifiers.Insert(0, new Domain.Models.ActiveModifier
        {
            Id = $"{command.ModifierId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            ModifierId = command.ModifierId,
            TriggeredBy = command.TriggeredBy,
            ActivatedAt = DateTimeOffset.UtcNow,
        });

        if (activeModifiers.Count > MaxActiveModifiers)
        {
            activeModifiers.RemoveRange(MaxActiveModifiers, activeModifiers.Count - MaxActiveModifiers);
        }

        await _repository.SaveActiveModifiersAsync(activeModifiers, cancellationToken);

        return BuildSnapshot(definitions, activeModifiers);
    }

    private static ModifiersSnapshot BuildSnapshot(
        IReadOnlyList<Domain.Models.ModifierDefinition> definitions,
        IReadOnlyList<Domain.Models.ActiveModifier> activeModifiers
    )
    {
        return new ModifiersSnapshot(
            definitions
                .Select(definition => new Contracts.ModifierDefinition(
                    definition.Id,
                    definition.Name,
                    definition.Cost,
                    definition.Description
                ))
                .ToArray(),
            activeModifiers
                .Select(modifier => new Contracts.ActiveModifier(
                    modifier.Id,
                    modifier.ModifierId,
                    modifier.ActivatedAt,
                    modifier.TriggeredBy
                ))
                .ToArray()
        );
    }
}
