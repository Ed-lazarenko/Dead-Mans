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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _dbContext;

    public DbGameModifierRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

}
