namespace GitClone.Application.Fetch;

public sealed record FetchRequest(string WorkingDirectory, string RemoteName, string? Branch) : IUseCaseRequest;
