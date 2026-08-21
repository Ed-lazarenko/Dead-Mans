namespace backend.Domain.Persistence;

public static class GameRoundTechnicalCancellationReasonValue
{
    public const string ExternalGameFailure = "external_game_failure";
    public const string StreamOrInfrastructureFailure = "stream_or_infrastructure_failure";
    public const string ApplicationError = "application_error";
    public const string OperatorError = "operator_error";
    public const string Other = "other";

    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(
        [
            ExternalGameFailure,
            StreamOrInfrastructureFailure,
            ApplicationError,
            OperatorError,
            Other
        ],
        StringComparer.Ordinal
    );
}
