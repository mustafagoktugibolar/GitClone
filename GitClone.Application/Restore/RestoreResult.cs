namespace GitClone.Application.Restore;

public sealed record RestoreResult(
    bool Succeeded,
    bool ShowUsage,
    string Message) : IUseCaseResult
{
    public static RestoreResult Usage(string message)
    {
        return new RestoreResult(false, true, message);
    }
}
