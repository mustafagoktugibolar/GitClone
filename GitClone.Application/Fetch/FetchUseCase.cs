using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Fetch;

/// <summary>
/// Copies commits and blobs for one branch (or, with no branch given, every branch) from a
/// remote's .ilos repository into ours, recording what was fetched under
/// refs/remotes/&lt;remote&gt;/&lt;branch&gt; without touching the working tree, index, or local
/// branches - exactly like `git fetch`.
/// </summary>
public sealed class FetchUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<FetchRequest, FetchResult>
{
    public async Task<FetchResult> ExecuteAsync(FetchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RemoteName))
        {
            return new FetchResult(false, "A remote name is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var (found, remotePath, error) = await RepositoryState.ResolveRemote(session, repositorySessionFactory, request.RemoteName);
        if (!found)
        {
            return new FetchResult(false, error!);
        }

        var remoteSession = repositorySessionFactory.CreateForPath(remotePath!);

        var branchesToFetch = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Branch))
        {
            branchesToFetch.Add(request.Branch);
        }
        else if (remoteSession.FileSystem.DirectoryExists(remoteSession.Context.HeadsPath))
        {
            branchesToFetch.AddRange(
                remoteSession.FileSystem.GetFiles(remoteSession.Context.HeadsPath)
                    .Select(Path.GetFileName)
                    .Where(name => name is not null)
                    .Select(name => name!));
        }

        if (branchesToFetch.Count == 0)
        {
            return new FetchResult(true, $"Remote '{request.RemoteName}' has no branches.");
        }

        var updatedRefs = new List<string>();
        foreach (var branch in branchesToFetch)
        {
            var remoteBranchRefPath = RepositoryState.ResolveBranchRefPath(remoteSession.Context, branch);
            var remoteCommitId = await RepositoryState.ReadReferenceValue(remoteSession.FileSystem, remoteBranchRefPath);
            if (string.IsNullOrWhiteSpace(remoteCommitId))
            {
                continue;
            }

            await RepositoryState.CopyCommitGraph(remoteSession, session, remoteCommitId);

            var trackingDirectory = Path.Combine(session.Context.RemotesPath, request.RemoteName);
            session.FileSystem.CreateDirectory(trackingDirectory);
            var trackingRefPath = Path.Combine(trackingDirectory, branch);
            await session.FileSystem.WriteAtomic(trackingRefPath, remoteCommitId);

            updatedRefs.Add($"{request.RemoteName}/{branch}");
        }

        return new FetchResult(true, $"Fetched {updatedRefs.Count} branch(es) from '{request.RemoteName}'.", updatedRefs);
    }
}
