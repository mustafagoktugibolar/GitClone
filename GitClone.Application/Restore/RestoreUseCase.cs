using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Restore;

public sealed class RestoreUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<RestoreRequest, RestoreResult>
{
    public async Task<RestoreResult> ExecuteAsync(RestoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetPath))
        {
            return RestoreResult.Usage("Path is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var relativePath = ResolveRelativePath(session.Context.RootPath, request.WorkingDirectory, request.TargetPath);

        if (request.StagedOnly)
        {
            var head = await RepositoryState.ReadHeadReference(session);
            var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
            var headCommit = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
            var headTree = await RepositoryState.ReadCommitTree(session, headCommit);

            if (headTree.TryGetValue(relativePath, out var headHash))
            {
                indexEntries[relativePath] = headHash;
            }
            else
            {
                indexEntries.Remove(relativePath);
            }

            await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, indexEntries);
            return new RestoreResult(true, false, $"Restored staged state for '{relativePath}'.");
        }

        if (!indexEntries.TryGetValue(relativePath, out var hash))
        {
            return new RestoreResult(false, false, $"Path is not tracked in index: {relativePath}");
        }

        var blobPath = Path.Combine(session.Context.ObjectsPath, hash);
        if (!session.FileSystem.FileExists(blobPath))
        {
            throw new InvalidOperationException($"Blob '{hash}' referenced by index is missing.");
        }

        var fullPath = RepositoryState.ToFullPath(session.Context.RootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            session.FileSystem.CreateDirectory(directory);
        }

        var content = await session.FileSystem.Read(blobPath);
        await session.FileSystem.WriteAtomic(fullPath, content);
        return new RestoreResult(true, false, $"Restored '{relativePath}' from index.");
    }

    private static string ResolveRelativePath(string rootPath, string workingDirectory, string targetPath)
    {
        var fullPath = Path.IsPathRooted(targetPath)
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(workingDirectory, targetPath));

        return RepositoryState.ToRelativePath(rootPath, fullPath);
    }
}
