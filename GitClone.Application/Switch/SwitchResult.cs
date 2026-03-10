namespace GitClone.Application.Switch;

public sealed record SwitchResult(
    bool Succeeded,
    bool ShowUsage,
    string Message,
    string? BranchName = null) : IUseCaseResult
{
    public static SwitchResult Usage(string message)
    {
        return new SwitchResult(false, true, message);
    }
}
