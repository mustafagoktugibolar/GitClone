namespace GitClone.Application.Pull;

public sealed record PullRequest(string WorkingDirectory, string RemoteName, string Branch) : IUseCaseRequest;
