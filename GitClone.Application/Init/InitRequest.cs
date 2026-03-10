namespace GitClone.Application.Init;

public sealed record InitRequest(string WorkingDirectory) : IUseCaseRequest;
