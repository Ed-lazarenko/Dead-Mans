namespace backend.Application.Abstractions.Auth;

public static class AuthRoleCodes
{
    public const string Viewer = "viewer";
    public const string Moderator = "moderator";
    public const string Admin = "admin";

    public const string ModeratorOrAdmin = Moderator + "," + Admin;

    public static readonly string[] Supported =
    [
        Viewer,
        Moderator,
        Admin
    ];
}
