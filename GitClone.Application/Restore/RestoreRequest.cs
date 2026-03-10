namespace GitClone.Application.Restore;

public sealed record RestoreRequest(
    string WorkingDirectory,
    string TargetPath,
    bool StagedOnly) : IUseCaseRequest;
