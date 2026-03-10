namespace GitClone.Application.Diff;

public sealed record DiffRequest(string WorkingDirectory, bool Cached) : IUseCaseRequest;
