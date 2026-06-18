using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public enum CreateDraftGameSetupOutcome
{
    Created,
    DraftAlreadyExists,
    InvalidTitle
}

public sealed record CreateDraftGameSetupResult(
    CreateDraftGameSetupOutcome Outcome,
    GameBoardSnapshot? Snapshot = null
);

public enum UpdateDraftGameSetupOutcome
{
    Updated,
    NoDraftFound,
    StaleVersion,
    InvalidTitle,
    InvalidRowLabels,
    InvalidColumnLabels,
    InvalidCells,
    InvalidEnabledModifiers,
    InvalidEnabledQuestions
}

public sealed record UpdateDraftGameSetupResult(
    UpdateDraftGameSetupOutcome Outcome,
    GameBoardSnapshot? Snapshot = null
);

public enum DeleteDraftGameSetupOutcome
{
    Deleted,
    NoDraftFound
}

public sealed record DeleteDraftGameSetupResult(DeleteDraftGameSetupOutcome Outcome);

public interface IGameSetupService
{
    Task<GameBoardSnapshot?> GetDraftSetupAsync(CancellationToken cancellationToken = default);

    Task<CreateDraftGameSetupResult> CreateDraftSetupAsync(
        string title,
        CancellationToken cancellationToken = default
    );

    Task<UpdateDraftGameSetupResult> UpdateDraftSetupAsync(
        GameSetupDraftUpdate update,
        CancellationToken cancellationToken = default
    );

    Task<DeleteDraftGameSetupResult> DeleteDraftSetupAsync(CancellationToken cancellationToken = default);
}
