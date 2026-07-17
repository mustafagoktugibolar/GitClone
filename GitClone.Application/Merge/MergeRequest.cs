namespace GitClone.Application.Merge;

public sealed record MergeRequest(string WorkingDirectory, string SourceBranch) : IUseCaseRequest;
