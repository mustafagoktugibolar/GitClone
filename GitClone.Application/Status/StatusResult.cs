namespace GitClone.Application.Status;

public sealed record StatusResult(
    IReadOnlyList<string> UntrackedFiles,
    IReadOnlyList<string> TrackedCleanFiles,
    IReadOnlyList<string> ModifiedFiles) : IUseCaseResult
{
    public bool IsWorkingTreeClean => UntrackedFiles.Count == 0 && ModifiedFiles.Count == 0;
}
