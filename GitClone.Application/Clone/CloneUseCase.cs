using GitClone.Core.Abstractions;
using GitClone.Application.Init;

namespace GitClone.Application.Clone;

public sealed class CloneUseCase(InitUseCase initUseCase, ICloneExecutor cloneExecutor) : IUseCase<CloneRequest, CloneResult>
{
    public async Task<CloneResult> ExecuteAsync(CloneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentException("url is required.");
        }

        var clone = await cloneExecutor.ExecuteAsync(
            new CloneExecutionRequest(
                request.Url,
                request.WorkingDirectory,
                request.ProjectName,
                request.Branch,
                request.Location));

        await initUseCase.ExecuteAsync(new InitRequest(clone.TargetDirectory));
        return new CloneResult(clone.RepositoryName, clone.BranchName, clone.TargetDirectory);
    }
}
