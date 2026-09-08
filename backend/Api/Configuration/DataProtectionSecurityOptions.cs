namespace backend.Api.Configuration;

public sealed class DataProtectionSecurityOptions
{
    public const string SectionName = "DataProtection";

    public string KeysDirectory { get; set; } = string.Empty;

    public static bool IsValidKeysDirectory(string? path, bool required)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return !required;
        }

        try
        {
            if (!Path.IsPathFullyQualified(path))
            {
                return false;
            }

            var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var rootPath = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(fullPath)!);
            return !string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase)
                && !File.Exists(fullPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
