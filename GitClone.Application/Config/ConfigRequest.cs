namespace GitClone.Application.Config;

public sealed record ConfigRequest(string WorkingDirectory, string[] Args) : IUseCaseRequest;
