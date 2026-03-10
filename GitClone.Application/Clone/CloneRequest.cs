namespace GitClone.Application.Clone;

public sealed record CloneRequest(
    string WorkingDirectory,
    string Url,
    string? ProjectName,
    string? Branch,
    string? Location) : IUseCaseRequest;
