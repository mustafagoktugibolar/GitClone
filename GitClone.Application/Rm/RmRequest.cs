namespace GitClone.Application.Rm;

public sealed record RmRequest(string WorkingDirectory, string TargetPath, bool Cached) : IUseCaseRequest;
