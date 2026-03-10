namespace GitClone.Application.Clone;

public sealed record CloneResult(string RepositoryName, string BranchName, string TargetDirectory) : IUseCaseResult;
