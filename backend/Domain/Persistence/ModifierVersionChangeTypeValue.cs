namespace backend.Domain.Persistence;

public static class ModifierVersionChangeTypeValue
{
    public const string Created = "created";
    public const string Edited = "edited";
    public const string CompatibilityCascade = "compatibility_cascade";
    public const string MigrationBaseline = "migration_baseline";

    public const string CheckSql =
        "change_type IN ('created','edited','compatibility_cascade','migration_baseline')";
}
