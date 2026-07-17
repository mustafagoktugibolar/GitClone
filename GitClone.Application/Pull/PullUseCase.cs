using GitClone.Application.Fetch;
using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Pull;

/// <summary>Fetches from a remote, then merges the fetched branch into the current branch using
/// the same core algorithm <c>MergeUseCase</c> uses - `pull` really is just `fetch` + `merge`.</summary>
public sealed class PullUseCase(IRepositorySessionFactory repositorySessionFactory, FetchUseCase fetchUseCase) : IUseCase<PullRequest, PullResult>
{
    public async Task<PullResult> ExecuteAsync(PullRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            return PullResult.Failed("A branch name is required.");
        }

        var fetchResult = await fetchUseCase.ExecuteAsync(new FetchRequest(request.WorkingDirectory, request.RemoteName, request.Branch));
        if (!fetchResult.Succeeded)
        {
            return PullResult.Failed(fetchResult.Message);
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var trackingRefPath = Path.Combine(session.Context.RemotesPath, request.RemoteName, request.Branch);
        var theirsCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, trackingRefPath);
        if (string.IsNullOrWhiteSpace(theirsCommitId))
        {
            return new PullResult(true, PullOutcome.AlreadyUpToDate, $"Remote branch '{request.RemoteName}/{request.Branch}' has no commits.");
        }

        var head = await RepositoryState.ReadHeadReference(session);
        var remoteLabel = $"{request.RemoteName}/{request.Branch}";
        var outcome = await CommitMerging.MergeIntoCurrentBranch(
            session, theirsCommitId, head.BranchName, remoteLabel, $"Merge remote-tracking branch '{remoteLabel}' into {head.BranchName}");

        return outcome.Kind switch
        {
            CommitMergeOutcomeKind.AlreadyUpToDate => new PullResult(true, PullOutcome.AlreadyUpToDate, "Already up to date."),
            CommitMergeOutcomeKind.FastForward => new PullResult(true, PullOutcome.FastForward, "Fast-forward.", outcome.CommitId),
            CommitMergeOutcomeKind.Merged => new PullResult(true, PullOutcome.Merged, $"Merged '{remoteLabel}'.", outcome.CommitId),
            CommitMergeOutcomeKind.Conflict => new PullResult(
                false,
                PullOutcome.Conflict,
                $"Pull has {outcome.Conflicts!.Count} conflicting file(s). Resolve them, then commit.",
                null,
                outcome.Conflicts),
            _ => PullResult.Failed("Unknown pull outcome.")
        };
    }
}
