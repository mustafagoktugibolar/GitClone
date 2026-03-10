namespace GitClone.Application.Status;

public sealed record StatusRequest(string WorkingDirectory) : IUseCaseRequest;
