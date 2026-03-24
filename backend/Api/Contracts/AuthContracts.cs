namespace backend.Api.Contracts;

public sealed record AuthSessionDto(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles
);

public sealed record ErrorResponse(string Error);
