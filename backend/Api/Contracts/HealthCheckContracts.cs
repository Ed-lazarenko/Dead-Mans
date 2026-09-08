namespace backend.Api.Contracts;

public static class HealthCheckContracts
{
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health/ready";

    public static class Names
    {
        public const string Database = "database";
        public const string ObjectStorage = "object-storage";
    }

    public static class Tags
    {
        public const string Ready = "ready";
    }
}
