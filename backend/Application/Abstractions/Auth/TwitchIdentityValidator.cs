namespace backend.Application.Abstractions.Auth;

public static class TwitchIdentityValidator
{
    public const int MaximumTwitchUserIdLength = 64;
    public const int MaximumLoginLength = 64;
    public const int MaximumDisplayNameLength = 64;

    public static bool IsValid(string? twitchUserId, string? login, string? displayName)
    {
        return HasRequiredValue(twitchUserId, MaximumTwitchUserIdLength)
            && twitchUserId!.All(char.IsAsciiDigit)
            && HasRequiredValue(login, MaximumLoginLength)
            && HasRequiredValue(displayName, MaximumDisplayNameLength);
    }

    private static bool HasRequiredValue(string? value, int maximumLength)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;
    }
}
