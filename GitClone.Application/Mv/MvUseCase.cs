using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Mv;

public sealed class MvUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<MvRequest, MvResult>
{
    public async Task<MvResult> ExecuteAsync(MvRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.Destination))
        {
            return new MvResult(false, "Both a source and destination path are required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var sourceRelative = ResolveRelativePath(session.Context.RootPath, request.WorkingDirectory, request.Source);
        var destinationRelative = ResolveRelativePath(session.Context.RootPath, request.WorkingDirectory, request.Destination);

        var sourceFullPath = RepositoryState.ToFullPath(session.Context.RootPath, sourceRelative);
        if (!session.FileSystem.FileExists(sourceFullPath))
        {
            return new MvResult(false, $"Source file does not exist: {sourceRelative}");
        }

        var destinationFullPath = RepositoryState.ToFullPath(session.Context.RootPath, destinationRelative);
        if (session.FileSystem.FileExists(destinationFullPath))
        {
            return new MvResult(false, $"Destination already exists: {destinationRelative}");
        }

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var wasTracked = indexEntries.Remove(sourceRelative, out var hash);

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            session.FileSystem.CreateDirectory(destinationDirectory);
        }

        session.FileSystem.MoveFile(sourceFullPath, destinationFullPath);

        if (wasTracked)
        {
            indexEntries[destinationRelative] = hash!;
            await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, indexEntries);
        }

        return new MvResult(
            true,
            wasTracked
                ? $"Renamed '{sourceRelative}' to '{destinationRelative}'."
                : $"Moved untracked file '{sourceRelative}' to '{destinationRelative}'.");
    }

    private static string ResolveRelativePath(string rootPath, string workingDirectory, string targetPath)
    {
        var fullPath = Path.IsPathRooted(targetPath)
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Combine(workingDirectory, targetPath));

        return RepositoryState.ToRelativePath(rootPath, fullPath);
    }
}
