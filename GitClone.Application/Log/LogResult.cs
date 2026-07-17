namespace GitClone.Application.Log;

public sealed record LogEntry(
    string CommitId,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTime CommittedAtUtc,
    bool IsMergeCommit = false);

public sealed record LogResult(
    bool Succeeded,
    bool ShowUsage,
    string BranchName,
    IReadOnlyList<LogEntry> Entries,
    string? Message = null) : IUseCaseResult
{
    public static LogResult Usage(string message)
    {
        return new LogResult(false, true, "unknown", [], message);
    }
}
