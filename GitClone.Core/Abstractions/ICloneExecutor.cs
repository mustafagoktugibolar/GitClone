namespace GitClone.Core.Abstractions;

public interface ICloneExecutor
{
    Task<CloneExecutionResult> ExecuteAsync(CloneExecutionRequest request);
}

public sealed record CloneExecutionRequest(
    string Url,
    string WorkingDirectory,
    string? ProjectName,
    string? Branch,
    string? Location);

public sealed record CloneExecutionResult(
    string RepositoryName,
    string BranchName,
    string TargetDirectory);
