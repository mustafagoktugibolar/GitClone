namespace GitClone.Application.Log;

public sealed record LogRequest(string WorkingDirectory, int? MaxCount = null) : IUseCaseRequest;
