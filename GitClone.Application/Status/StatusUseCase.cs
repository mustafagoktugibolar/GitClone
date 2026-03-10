using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Application.Status;

public sealed class StatusUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<StatusRequest, StatusResult>
{
    public async Task<StatusResult> ExecuteAsync(StatusRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var indexEntries = await LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var trackedClean = new List<string>();
        var modified = new List<string>();

        foreach (var entry in indexEntries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var relativePath = ToRelativePath(session.Context.RootPath, entry.Key);
            var fullPath = Path.IsPathRooted(entry.Key)
                ? Path.GetFullPath(entry.Key)
                : Path.GetFullPath(Path.Combine(session.Context.RootPath, entry.Key));

            if (!session.FileSystem.FileExists(fullPath))
            {
                modified.Add(relativePath);
                continue;
            }

            var content = await session.FileSystem.Read(fullPath);
            var currentHash = session.HashService.ComputeSha1(content);

            if (string.Equals(currentHash, entry.Value, StringComparison.OrdinalIgnoreCase))
            {
                trackedClean.Add(relativePath);
            }
            else
            {
                modified.Add(relativePath);
            }
        }

        var untracked = CollectUntrackedFiles(session, indexEntries.Keys);

        return new StatusResult(
            untracked,
            trackedClean.OrderBy(path => path, StringComparer.Ordinal).ToList(),
            modified.OrderBy(path => path, StringComparer.Ordinal).ToList());
    }

    private static async Task<Dictionary<string, string>> LoadIndexEntries(IFileSystem fileSystem, string indexPath)
    {
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!fileSystem.FileExists(indexPath))
        {
            return entries;
        }

        var lines = await fileSystem.ReadAllLines(indexPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = line.IndexOf(' ');
            if (separator <= 0 || separator >= line.Length - 1)
            {
                continue;
            }

            var filePath = line[..separator].Trim();
            var hash = line[(separator + 1)..].Trim();
            if (filePath.Length == 0 || hash.Length == 0)
            {
                continue;
            }

            entries[filePath] = hash;
        }

        return entries;
    }

    private static IReadOnlyList<string> CollectUntrackedFiles(IRepositorySession session, IEnumerable<string> trackedPaths)
    {
        var rootPath = session.Context.RootPath;
        if (!session.FileSystem.DirectoryExists(rootPath))
        {
            return [];
        }

        var normalizedTrackedPaths = trackedPaths
            .Select(path => ToRelativePath(rootPath, path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var untracked = new List<string>();

        foreach (var fullPath in session.FileSystem.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = NormalizePath(Path.GetRelativePath(rootPath, fullPath));
            if (relativePath.Equals(".ilos", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith(".ilos/", StringComparison.OrdinalIgnoreCase) ||
                relativePath.Equals(".ilosignore", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (session.IgnoreService.IsIgnored(fullPath))
            {
                continue;
            }

            if (!normalizedTrackedPaths.Contains(relativePath))
            {
                untracked.Add(relativePath);
            }
        }

        return untracked.OrderBy(path => path, StringComparer.Ordinal).ToList();
    }

    private static string ToRelativePath(string rootPath, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return NormalizePath(Path.GetRelativePath(rootPath, Path.GetFullPath(path)));
        }

        return NormalizePath(path);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
