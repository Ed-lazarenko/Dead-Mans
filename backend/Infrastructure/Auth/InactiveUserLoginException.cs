namespace backend.Infrastructure.Auth;

public sealed class InactiveUserLoginException : Exception
{
    public InactiveUserLoginException(Guid userId)
        : base($"Inactive user '{userId}' cannot sign in.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
