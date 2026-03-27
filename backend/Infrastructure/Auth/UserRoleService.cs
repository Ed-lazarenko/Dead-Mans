using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Auth;

public sealed class UserRoleService : IUserRoleService
{
    private const string ViewerRoleCode = AuthRoleCodes.Viewer;

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserRoleService> _logger;

    public UserRoleService(ApplicationDbContext dbContext, ILogger<UserRoleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string[]> EnsureEffectiveRolesAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var utcNow = DateTime.UtcNow;

        var viewerRole = await _dbContext.Roles
            .Where(x => x.Code == ViewerRoleCode)
            .Select(x => new { x.Id, x.Code })
            .SingleOrDefaultAsync(cancellationToken);

        if (viewerRole is null)
        {
            _logger.LogError(AppMessages.Logs.ViewerRoleMissingFromTable, ViewerRoleCode);
            throw new InvalidOperationException(AppMessages.Exceptions.ViewerRoleMissing);
        }

        var existingAssignments = await _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new UserRoleAssignmentSnapshot
                {
                    UserRole = userRole,
                    RoleCode = role.Code
                }
            )
            .ToListAsync(cancellationToken);

        var viewerAssignment = existingAssignments.FirstOrDefault(x => x.RoleCode == ViewerRoleCode)?.UserRole;
        if (viewerAssignment is null)
        {
            _dbContext.UserRoles.Add(
                new UserRole
                {
                    UserId = userId,
                    RoleId = viewerRole.Id,
                    AssignedAtUtc = utcNow,
                    ExpiresAtUtc = null
                }
            );

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (viewerAssignment.ExpiresAtUtc.HasValue && viewerAssignment.ExpiresAtUtc <= utcNow)
        {
            viewerAssignment.AssignedAtUtc = utcNow;
            viewerAssignment.ExpiresAtUtc = null;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetEffectiveRolesAsync(userId, cancellationToken);
    }

    public async Task<string[]> GetEffectiveRolesAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var utcNow = DateTime.UtcNow;
        var effectiveRoles = await _dbContext.UserRoles
            .Where(x => x.UserId == userId && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > utcNow))
            .Join(_dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!effectiveRoles.Contains(ViewerRoleCode, StringComparer.Ordinal))
        {
            effectiveRoles.Add(ViewerRoleCode);
        }

        return NormalizeRoles(effectiveRoles);
    }

    private static string[] NormalizeRoles(IEnumerable<string> roleCodes)
    {
        return roleCodes
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code == ViewerRoleCode ? 0 : 1)
            .ThenBy(code => code, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class UserRoleAssignmentSnapshot
    {
        public UserRole UserRole { get; init; } = default!;

        public string RoleCode { get; init; } = string.Empty;
    }
}
