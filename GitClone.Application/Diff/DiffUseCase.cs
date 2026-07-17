using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Diff;

public sealed class DiffUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<DiffRequest, DiffResult>
{
    public async Task<DiffResult> ExecuteAsync(DiffRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);

        IReadOnlyList<DiffEntry> entries = request.Cached
            ? await BuildCachedDiff(session, indexEntries)
            : await BuildWorkingTreeDiff(session, indexEntries);

        return new DiffResult(true, false, entries);
    }

    private static async Task<IReadOnlyList<DiffEntry>> BuildCachedDiff(
        IRepositorySession session,
        IReadOnlyDictionary<string, string> indexEntries)
    {
        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var headCommit = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var headTree = await RepositoryState.ReadCommitTree(session, headCommit);

        var allPaths = indexEntries.Keys
            .Concat(headTree.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.Ordinal);

        var entries = new List<DiffEntry>();
        foreach (var path in allPaths)
        {
            var inIndex = indexEntries.TryGetValue(path, out var indexHash);
            var inHead = headTree.TryGetValue(path, out var headHash);

            if (inIndex && !inHead)
            {
                entries.Add(new DiffEntry("A", path));
                continue;
            }

            if (!inIndex && inHead)
            {
                entries.Add(new DiffEntry("D", path));
                continue;
            }

            if (!string.Equals(indexHash, headHash, StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new DiffEntry("M", path));
            }
        }

        return entries;
    }

    private static async Task<IReadOnlyList<DiffEntry>> BuildWorkingTreeDiff(
        IRepositorySession session,
        IReadOnlyDictionary<string, string> indexEntries)
    {
        var entries = new List<DiffEntry>();
        var rootPath = session.Context.RootPath;

        foreach (var item in indexEntries.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            var fullPath = RepositoryState.ToFullPath(rootPath, item.Key);
            if (!session.FileSystem.FileExists(fullPath))
            {
                entries.Add(new DiffEntry("D", item.Key));
                continue;
            }

            var content = await session.FileSystem.ReadBytes(fullPath);
            var currentHash = session.HashService.ComputeSha1(content);
            if (!string.Equals(currentHash, item.Value, StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new DiffEntry("M", item.Key));
            }
        }

        var tracked = indexEntries.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var fullPath in session.FileSystem.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = RepositoryState.NormalizePath(Path.GetRelativePath(rootPath, fullPath));
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

            if (!tracked.Contains(relativePath))
            {
                entries.Add(new DiffEntry("??", relativePath));
            }
        }

        return entries
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToList();
    }
}
