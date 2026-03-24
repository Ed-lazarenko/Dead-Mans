using backend.Application.Abstractions.Auth;

namespace backend.Application.Features.Auth;

public interface IAuthSessionService
{
    Task<AuthSession?> GetSessionAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AuthSessionService : IAuthSessionService
{
    private readonly IAuthUserReader _authUserReader;
    private readonly IUserRoleService _userRoleService;

    public AuthSessionService(IAuthUserReader authUserReader, IUserRoleService userRoleService)
    {
        _authUserReader = authUserReader;
        _userRoleService = userRoleService;
    }

    public async Task<AuthSession?> GetSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _authUserReader.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await _userRoleService.GetEffectiveRolesAsync(user.UserId, cancellationToken);
        return new AuthSession(user.UserId, user.DisplayName, roles);
    }
}
