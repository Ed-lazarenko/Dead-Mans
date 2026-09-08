using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.Scoring;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GameModifierActivationContract = backend.Application.Contracts.GameModifierActivation;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameModifierRepository : IGameModifierRepository
{
    public async Task<ModifierHistoryPage<ModifierHistorySummary>> GetHistoryAsync(
        ModifierHistoryQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadHistoryAsync(query, cancellationToken);

    public async Task<ModifierHistoryPage<ModifierVersionSummary>?> GetVersionsAsync(
        Guid modifierId,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionsAsync(modifierId, query, cancellationToken);

    public async Task<ModifierVersionDetail?> GetVersionAsync(
        Guid modifierId,
        int revision,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionAsync(modifierId, revision, cancellationToken);

    public async Task<ModifierHistoryPage<ModifierVersionGameSummary>?> GetVersionGamesAsync(
        Guid modifierId,
        int revision,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default
    ) => await new ModifierHistoryReadProjection(_dbContext)
        .LoadVersionGamesAsync(modifierId, revision, query, cancellationToken);

}
