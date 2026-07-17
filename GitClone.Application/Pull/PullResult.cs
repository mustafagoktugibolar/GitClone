using GitClone.Application.Shared;

namespace GitClone.Application.Pull;

public enum PullOutcome
{
    AlreadyUpToDate,
    FastForward,
    Merged,
    Conflict,
    Failed
}

public sealed record PullResult(
    bool Succeeded,
    PullOutcome Outcome,
    string Message,
    string? CommitId = null,
    IReadOnlyList<MergeConflict>? Conflicts = null) : IUseCaseResult
{
    public static PullResult Failed(string message) => new(false, PullOutcome.Failed, message);
}
