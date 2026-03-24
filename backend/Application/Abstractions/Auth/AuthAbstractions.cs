namespace backend.Application.Abstractions.Auth;

public sealed record AuthUserSummary(Guid UserId, string DisplayName, bool IsActive);

public sealed record AuthSession(Guid UserId, string DisplayName, IReadOnlyList<string> Roles);

public sealed record TwitchAuthenticatedUser(
    Guid UserId,
    string TwitchUserId,
    string DisplayName,
    string[] Roles,
    bool IsNewUser
);

public interface IAuthUserReader
{
    Task<AuthUserSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IUserRoleService
{
    Task<string[]> GetEffectiveRolesAsync(Guid userId, CancellationToken cancellationToken);

    Task<string[]> EnsureEffectiveRolesAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ITwitchLoginService
{
    string BuildAuthorizeUrl(string state);

    Task<TwitchAuthenticatedUser> AuthenticateAsync(string code, CancellationToken cancellationToken);
}
