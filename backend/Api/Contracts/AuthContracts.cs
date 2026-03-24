namespace backend.Api.Contracts;

public enum AuthRole
{
    Admin,
    Moderator,
    Viewer
}

public sealed record AuthSessionDto(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<AuthRole> Roles
);

public sealed record ErrorResponse(string Error);
