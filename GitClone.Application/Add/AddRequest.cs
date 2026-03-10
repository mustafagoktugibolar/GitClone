namespace GitClone.Application.Add;

public sealed record AddRequest(string WorkingDirectory, string TargetPath) : IUseCaseRequest;
