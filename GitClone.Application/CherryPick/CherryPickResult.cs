using GitClone.Application.Shared;

namespace GitClone.Application.CherryPick;

public enum CherryPickOutcome
{
    Applied,
    Conflict,
    Failed
}

public sealed record CherryPickResult(
    bool Succeeded,
    CherryPickOutcome Outcome,
    string Message,
    string? CommitId = null,
    IReadOnlyList<MergeConflict>? Conflicts = null) : IUseCaseResult
{
    public static CherryPickResult Failed(string message) => new(false, CherryPickOutcome.Failed, message);
}
