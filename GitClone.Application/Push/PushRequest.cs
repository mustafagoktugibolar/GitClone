namespace GitClone.Application.Push;

public sealed record PushRequest(string WorkingDirectory, string RemoteName, string Branch) : IUseCaseRequest;
