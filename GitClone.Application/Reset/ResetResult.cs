namespace GitClone.Application.Reset;

public sealed record ResetResult(
    bool Succeeded,
    bool ShowUsage,
    string Message,
    string? CommitId = null) : IUseCaseResult
{
    public static ResetResult Usage(string message)
    {
        return new ResetResult(false, true, message);
    }
}
