using GitClone.Application.Shared;

namespace GitClone.Application.Stash;

public enum StashOutcome
{
    Saved,
    NoChanges,
    Applied,
    Conflict,
    Listed,
    Dropped,
    Failed
}

public sealed record StashEntrySummary(int Index, string Message, string? BaseCommitId, DateTime CreatedAtUtc);

public sealed record StashResult(
    bool Succeeded,
    StashOutcome Outcome,
    string Message,
    IReadOnlyList<StashEntrySummary>? Entries = null,
    IReadOnlyList<MergeConflict>? Conflicts = null) : IUseCaseResult
{
    public static StashResult Failed(string message) => new(false, StashOutcome.Failed, message);
}
