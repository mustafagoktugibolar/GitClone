using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Rm;

public sealed class RmUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<RmRequest, RmResult>
{
    public async Task<RmResult> ExecuteAsync(RmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetPath))
        {
            return new RmResult(false, "A file path is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var relativePath = ResolveRelativePath(session.Context.RootPath, request.WorkingDirectory, request.TargetPath);
        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);

        if (!indexEntries.Remove(relativePath))
        {
            return new RmResult(false, $"Path is not tracked in index: {relativePath}");
        }

        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, indexEntries);

        if (!request.Cached)
        {
            var fullPath = RepositoryState.ToFullPath(session.Context.RootPath, relativePath);
            if (session.FileSystem.FileExists(fullPath))
            {
                session.FileSystem.DeleteFile(fullPath);
            }
        }

        return new RmResult(
            true,
            request.Cached
                ? $"Removed '{relativePath}' from index (working tree file kept)."
                : $"Removed '{relativePath}'.");
    }

    private static string ResolveRelativePath(string rootPath, string workingDirectory, string targetPath)
    {
        var fullPath = Path.IsPathRooted(targetPath)
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(workingDirectory, targetPath));

        return RepositoryState.ToRelativePath(rootPath, fullPath);
    }
}
