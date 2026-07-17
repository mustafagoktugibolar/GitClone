using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Merge;

public sealed class MergeUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<MergeRequest, MergeResult>
{
    public async Task<MergeResult> ExecuteAsync(MergeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceBranch))
        {
            return MergeResult.Failed("A branch name is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var head = await RepositoryState.ReadHeadReference(session);
        if (string.Equals(head.BranchName, request.SourceBranch, StringComparison.Ordinal))
        {
            return MergeResult.Failed("Cannot merge a branch into itself.");
        }

        var sourceBranchRefPath = RepositoryState.ResolveBranchRefPath(session.Context, request.SourceBranch);
        if (!session.FileSystem.FileExists(sourceBranchRefPath))
        {
            return MergeResult.Failed($"Branch not found: {request.SourceBranch}");
        }

        var theirsCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, sourceBranchRefPath);
        if (string.IsNullOrWhiteSpace(theirsCommitId))
        {
            return new MergeResult(true, MergeOutcome.AlreadyUpToDate, $"Branch '{request.SourceBranch}' has no commits.");
        }

        var outcome = await CommitMerging.MergeIntoCurrentBranch(
            session, theirsCommitId, head.BranchName, request.SourceBranch,
            $"Merge branch '{request.SourceBranch}' into {head.BranchName}");

        return outcome.Kind switch
        {
            CommitMergeOutcomeKind.AlreadyUpToDate =>
                new MergeResult(true, MergeOutcome.AlreadyUpToDate, "Already up to date."),
            CommitMergeOutcomeKind.FastForward =>
                new MergeResult(true, MergeOutcome.FastForward, "Fast-forward merge.", outcome.CommitId),
            CommitMergeOutcomeKind.Merged =>
                new MergeResult(true, MergeOutcome.Merged, $"Merged '{request.SourceBranch}' into '{head.BranchName}'.", outcome.CommitId),
            CommitMergeOutcomeKind.Conflict =>
                new MergeResult(
                    false,
                    MergeOutcome.Conflict,
                    $"Merge of '{request.SourceBranch}' has {outcome.Conflicts!.Count} conflicting file(s). Resolve them, then commit.",
                    null,
                    outcome.Conflicts),
            _ => MergeResult.Failed("Unknown merge outcome.")
        };
    }
}
