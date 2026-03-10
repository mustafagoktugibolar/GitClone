namespace GitClone.Application.Commit;

public sealed record CommitRequest(string WorkingDirectory, string Message) : IUseCaseRequest;
