using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Push;

/// <summary>
/// Copies our branch's commits and blobs into a remote's .ilos repository and fast-forwards its
/// branch ref - rejecting the push (like `git push`'s non-fast-forward rejection) if the remote
/// has commits we don't have locally, since blindly overwriting would drop that work. Never
/// touches the remote's working tree or index, matching real git's push semantics.
/// </summary>
public sealed class PushUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<PushRequest, PushResult>
{
    public async Task<PushResult> ExecuteAsync(PushRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Branch))
        {
            return new PushResult(false, "A branch name is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var (found, remotePath, error) = await RepositoryState.ResolveRemote(session, repositorySessionFactory, request.RemoteName);
        if (!found)
        {
            return new PushResult(false, error!);
        }

        var localBranchRefPath = RepositoryState.ResolveBranchRefPath(session.Context, request.Branch);
        if (!session.FileSystem.FileExists(localBranchRefPath))
        {
            return new PushResult(false, $"Branch not found: {request.Branch}");
        }

        var localCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, localBranchRefPath);
        if (string.IsNullOrWhiteSpace(localCommitId))
        {
            return new PushResult(false, $"Branch '{request.Branch}' has no commits.");
        }

        var remoteSession = repositorySessionFactory.CreateForPath(remotePath!);
        remoteSession.FileSystem.CreateDirectory(remoteSession.Context.HeadsPath);
        var remoteBranchRefPath = RepositoryState.ResolveBranchRefPath(remoteSession.Context, request.Branch);
        var remoteCommitId = await RepositoryState.ReadReferenceValue(remoteSession.FileSystem, remoteBranchRefPath);

        if (string.Equals(remoteCommitId, localCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return new PushResult(true, "Everything up-to-date.");
        }

        if (!string.IsNullOrWhiteSpace(remoteCommitId))
        {
            var localAncestors = await RepositoryState.GetAncestors(session, localCommitId);
            if (!localAncestors.Contains(remoteCommitId))
            {
                return new PushResult(
                    false,
                    $"Updates were rejected: '{request.RemoteName}' has work that isn't present locally. Fetch first.");
            }
        }

        await RepositoryState.CopyCommitGraph(session, remoteSession, localCommitId);
        await RepositoryState.WriteReferenceValue(remoteSession.FileSystem, remoteBranchRefPath, localCommitId);

        return new PushResult(true, $"Pushed '{request.Branch}' to '{request.RemoteName}'.");
    }
}
