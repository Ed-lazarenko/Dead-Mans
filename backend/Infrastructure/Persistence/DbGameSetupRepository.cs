using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Configuration;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameSetupRepository : IGameSetupRepository
{
    private const string SingleDraftConstraintName =
        PostgresUniqueViolation.GamesSingleDraft;

    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbGameSetupRepository> _logger;
    private readonly TimeProvider _timeProvider;

    public DbGameSetupRepository(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        ILogger<DbGameSetupRepository> logger,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');
        _logger = logger;
        _timeProvider = timeProvider;
    }
}
