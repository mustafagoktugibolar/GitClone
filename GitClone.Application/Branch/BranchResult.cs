namespace GitClone.Application.Branch;

public enum BranchOperation
{
    Usage,
    List,
    Create,
    Delete,
    Rename
}

public sealed record BranchResult(
    BranchOperation Operation,
    bool Succeeded,
    string Message,
    string? HeadBranch = null,
    IReadOnlyList<string>? Branches = null) : IUseCaseResult;
