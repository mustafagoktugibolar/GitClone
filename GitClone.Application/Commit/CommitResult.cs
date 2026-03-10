namespace GitClone.Application.Commit;

public sealed record CommitResult(
    bool Succeeded,
    bool ShowUsage,
    bool NothingToCommit,
    string Message,
    string? CommitId = null,
    string? BranchName = null,
    int ChangedFiles = 0) : IUseCaseResult
{
    public static CommitResult Usage(string message)
    {
        return new CommitResult(false, true, false, message);
    }

    public static CommitResult NoChanges(string message = "Nothing to commit.")
    {
        return new CommitResult(false, false, true, message);
    }
}
