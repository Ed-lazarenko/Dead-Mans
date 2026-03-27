namespace backend.Infrastructure.Configuration;

/// <summary>
/// Well-known configuration keys and connection string names used outside of typed options sections.
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Heroku-style full database URL (optional alternative to <see cref="ConnectionStringNames.Default"/>).
    /// </summary>
    public const string DatabaseUrlEnvironmentVariable = "DATABASE_URL";
}

/// <summary>
/// Names under the <c>ConnectionStrings</c> configuration section.
/// </summary>
public static class ConnectionStringNames
{
    public const string Default = "DefaultConnection";
}
