using System.Text.RegularExpressions;

namespace backend.Domain.Persistence;

public static partial class TeamNameValue
{
    public const int MaxLength = 48;

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = WhitespaceRegex().Replace(value.Trim(), " ");
        return normalized.Length == 0 ? null : normalized;
    }

    public static bool IsValid(string? value)
    {
        var normalized = Normalize(value);
        return normalized is null || normalized.Length <= MaxLength;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
