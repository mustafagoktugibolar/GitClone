namespace GitClone.Application.Diff;

public sealed record DiffEntry(string Status, string Path);

public sealed record DiffResult(
    bool Succeeded,
    bool ShowUsage,
    IReadOnlyList<DiffEntry> Entries,
    string? Message = null) : IUseCaseResult
{
    public static DiffResult Usage(string message)
    {
        return new DiffResult(false, true, [], message);
    }
}
