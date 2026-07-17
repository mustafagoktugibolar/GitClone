using GitClone.Application.Shared;

namespace GitClone.Application.Rebase;

public enum RebaseOutcome
{
    AlreadyUpToDate,
    Rebased,
    Conflict,
    Failed
}

public sealed record RebaseResult(
    bool Succeeded,
    RebaseOutcome Outcome,
    string Message,
    int CommitsReplayed = 0,
    IReadOnlyList<MergeConflict>? Conflicts = null) : IUseCaseResult
{
    public static RebaseResult Failed(string message) => new(false, RebaseOutcome.Failed, message);
}
