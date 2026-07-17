using GitClone.Application.Shared;

namespace GitClone.Application.Merge;

public enum MergeOutcome
{
    AlreadyUpToDate,
    FastForward,
    Merged,
    Conflict,
    Failed
}

public sealed record MergeResult(
    bool Succeeded,
    MergeOutcome Outcome,
    string Message,
    string? CommitId = null,
    IReadOnlyList<MergeConflict>? Conflicts = null) : IUseCaseResult
{
    public static MergeResult Failed(string message) => new(false, MergeOutcome.Failed, message);
}
