namespace backend.Domain.Persistence;

/// <summary>
/// Common values for <see cref="backend.Data.Entities.MediaAsset"/> string columns.
/// </summary>
public static class MediaAssetPersistence
{
    public const string ScopePrivate = "private";
    public const string StatusPending = "pending";
    public const string StatusActive = "active";

    /// <summary>CHECK fragment for <c>CK_media_assets_scope_allowed</c>.</summary>
    public static string CheckSqlAllowedScopes { get; } =
        $"\"Scope\" IN ('{ScopePrivate}')";

    /// <summary>CHECK fragment for <c>CK_media_assets_status_allowed</c>.</summary>
    public static string CheckSqlAllowedStatuses { get; } =
        $"\"Status\" IN ('{StatusPending}','{StatusActive}')";
}
