namespace GitClone.Application.Reset;

public enum ResetMode
{
    Soft,
    Mixed,
    Hard
}

public sealed record ResetRequest(
    string WorkingDirectory,
    ResetMode Mode,
    string? TargetSpec) : IUseCaseRequest;
