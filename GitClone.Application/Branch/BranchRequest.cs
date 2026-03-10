namespace GitClone.Application.Branch;

public sealed record BranchRequest(string WorkingDirectory, string[] Args) : IUseCaseRequest;
