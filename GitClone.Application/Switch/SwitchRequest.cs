namespace GitClone.Application.Switch;

public sealed record SwitchRequest(
    string WorkingDirectory,
    string TargetBranch,
    bool CreateIfMissing) : IUseCaseRequest;
