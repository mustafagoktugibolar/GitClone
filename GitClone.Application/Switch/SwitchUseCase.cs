using GitClone.Application.Shared;
using GitClone.Application.Status;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Switch;

public sealed class SwitchUseCase(
    IRepositorySessionFactory repositorySessionFactory,
    StatusUseCase statusUseCase) : IUseCase<SwitchRequest, SwitchResult>
{
    public async Task<SwitchResult> ExecuteAsync(SwitchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetBranch))
        {
            return SwitchResult.Usage("Branch name is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();
        session.FileSystem.CreateDirectory(session.Context.CommitsPath);
        session.FileSystem.CreateDirectory(session.Context.HeadsPath);

        var status = await statusUseCase.ExecuteAsync(new StatusRequest(request.WorkingDirectory));
        if (!status.IsWorkingTreeClean)
        {
            return new SwitchResult(
                false,
                false,
                "Working tree has local changes. Commit or restore changes before switching.");
        }

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var currentHeadCommit = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var currentHeadTree = await RepositoryState.ReadCommitTree(session, currentHeadCommit);

        if (!HasSameTree(indexEntries, currentHeadTree))
        {
            return new SwitchResult(
                false,
                false,
                "Index has staged changes. Commit or reset changes before switching.");
        }

        var targetRefPath = Path.Combine(session.Context.HeadsPath, request.TargetBranch);
        if (request.CreateIfMissing)
        {
            if (session.FileSystem.FileExists(targetRefPath))
            {
                return new SwitchResult(false, false, $"Branch already exists: {request.TargetBranch}");
            }

            await session.FileSystem.WriteAtomic(targetRefPath, currentHeadCommit ?? string.Empty);
        }

        if (!session.FileSystem.FileExists(targetRefPath))
        {
            return new SwitchResult(false, false, $"Branch not found: {request.TargetBranch}");
        }

        var targetCommit = await RepositoryState.ReadReferenceValue(session.FileSystem, targetRefPath);
        var targetTree = await RepositoryState.ReadCommitTree(session, targetCommit);

        await RepositoryState.ApplyTreeToWorkingTree(session, targetTree, indexEntries.Keys);
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, targetTree);
        await session.FileSystem.WriteAtomic(session.Context.HEADPath, $"ref: refs/heads/{request.TargetBranch}");

        return new SwitchResult(true, false, $"Switched to branch '{request.TargetBranch}'.", request.TargetBranch);
    }

    private static bool HasSameTree(
        IReadOnlyDictionary<string, string> first,
        IReadOnlyDictionary<string, string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (var item in first)
        {
            if (!second.TryGetValue(item.Key, out var otherHash))
            {
                return false;
            }

            if (!string.Equals(item.Value, otherHash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
