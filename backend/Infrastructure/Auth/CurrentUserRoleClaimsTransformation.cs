using System.Security.Claims;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;

namespace backend.Infrastructure.Auth;

public sealed class CurrentUserRoleClaimsTransformation : IClaimsTransformation
{
    private readonly IAuthUserReader _authUserReader;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<CurrentUserRoleClaimsTransformation> _logger;

    public CurrentUserRoleClaimsTransformation(
        IAuthUserReader authUserReader,
        IUserRoleService userRoleService,
        ILogger<CurrentUserRoleClaimsTransformation> logger
    )
    {
        _authUserReader = authUserReader;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var transformedPrincipal = new ClaimsPrincipal();
        ClaimsIdentity? transformedIdentity = null;

        foreach (var identity in principal.Identities)
        {
            var clonedIdentity = new ClaimsIdentity(identity);
            if (transformedIdentity is null && identity.IsAuthenticated)
            {
                transformedIdentity = clonedIdentity;
            }

            transformedPrincipal.AddIdentity(clonedIdentity);
        }

        if (transformedIdentity is null)
        {
            return principal;
        }

        ClearRoleClaims(transformedIdentity);

        var userIdValue = transformedIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            _logger.LogDebug(AppMessages.Logs.RoleClaimsSkipHydrationMissingGuid);
            return transformedPrincipal;
        }

        var user = await _authUserReader.FindByIdAsync(userId, CancellationToken.None);
        if (user is null || !user.IsActive)
        {
            _logger.LogDebug(AppMessages.Logs.RoleClaimsSkipHydrationInactiveUser, userId);
            return transformedPrincipal;
        }

        var roles = await _userRoleService.GetEffectiveRolesAsync(userId, CancellationToken.None);
        foreach (var role in roles.Where(IsSupportedRoleCode))
        {
            transformedIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return transformedPrincipal;
    }

    private static void ClearRoleClaims(ClaimsIdentity identity)
    {
        foreach (var claim in identity.FindAll(ClaimTypes.Role).ToArray())
        {
            identity.RemoveClaim(claim);
        }
    }

    private static bool IsSupportedRoleCode(string roleCode)
    {
        return AuthRoleCodes.Supported.Contains(roleCode, StringComparer.Ordinal);
    }
}
