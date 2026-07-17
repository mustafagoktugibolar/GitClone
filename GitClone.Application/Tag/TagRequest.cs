namespace GitClone.Application.Tag;

public sealed record TagRequest(string WorkingDirectory, string? Name, string? DeleteName) : IUseCaseRequest;
