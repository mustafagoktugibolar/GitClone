using System.Text.Json;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Application.Shared;

internal static class RepositoryState
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<Dictionary<string, string>> LoadIndexEntries(IFileSystem fileSystem, string indexPath)
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

            var filePath = NormalizePath(line[..separator].Trim());
            var hash = line[(separator + 1)..].Trim();
            if (filePath.Length == 0 || hash.Length == 0)
            {
                continue;
            }

            entries[filePath] = hash;
        }

        return entries;
    }

    public static Task SaveIndexEntries(IFileSystem fileSystem, string indexPath, IReadOnlyDictionary<string, string> entries)
    {
        var lines = entries
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => $"{NormalizePath(kvp.Key)} {kvp.Value}")
            .ToArray();

        var content = lines.Length == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
        return fileSystem.WriteAtomic(indexPath, content);
    }

    public static async Task<HeadReference> ReadHeadReference(IRepositorySession session)
    {
        if (!session.FileSystem.FileExists(session.Context.HEADPath))
        {
            throw new InvalidOperationException("HEAD is missing.");
        }

        var text = (await session.FileSystem.Read(session.Context.HEADPath)).Trim();
        if (text.Length == 0)
        {
            throw new InvalidOperationException("HEAD is empty.");
        }

        var parts = text.Split(':', 2);
        var referencePath = parts.Length == 2 ? parts[1].Trim() : text;
        var branchName = referencePath.Split('/').LastOrDefault() ?? "unknown";

        return new HeadReference(referencePath, branchName);
    }

    public static string ResolveReferencePath(IRepositoryContext context, string referencePath)
    {
        var normalized = referencePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(context.IlosPath, normalized));
    }

    public static async Task<string?> ReadReferenceValue(IFileSystem fileSystem, string path)
    {
        if (!fileSystem.FileExists(path))
        {
            return null;
        }

        var value = (await fileSystem.Read(path)).Trim();
        return value.Length == 0 ? null : value;
    }

    public static Task WriteReferenceValue(IFileSystem fileSystem, string path, string? value)
    {
        return fileSystem.WriteAtomic(path, value?.Trim() ?? string.Empty);
    }

    public static async Task<StoredCommit?> ReadCommit(IRepositorySession session, string commitId)
    {
        var commitPath = Path.Combine(session.Context.CommitsPath, $"{commitId}.json");
        if (!session.FileSystem.FileExists(commitPath))
        {
            return null;
        }

        var content = await session.FileSystem.Read(commitPath);
        return JsonSerializer.Deserialize<StoredCommit>(content, JsonOptions);
    }

    public static async Task<Dictionary<string, string>> ReadCommitTree(IRepositorySession session, string? commitId)
    {
        if (string.IsNullOrWhiteSpace(commitId))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var commit = await ReadCommit(session, commitId);
        if (commit == null)
        {
            throw new InvalidOperationException($"Commit '{commitId}' is missing.");
        }

        return new Dictionary<string, string>(commit.Files, StringComparer.OrdinalIgnoreCase);
    }

    public static async Task ApplyTreeToWorkingTree(
        IRepositorySession session,
        IReadOnlyDictionary<string, string> targetTree,
        IEnumerable<string> trackedPathsToReconcile)
    {
        var rootPath = Path.GetFullPath(session.Context.RootPath);
        var tracked = trackedPathsToReconcile
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var trackedPath in tracked)
        {
            if (targetTree.ContainsKey(trackedPath))
            {
                continue;
            }

            var fullPath = ToFullPath(rootPath, trackedPath);
            if (session.FileSystem.FileExists(fullPath))
            {
                session.FileSystem.DeleteFile(fullPath);
            }
        }

        foreach (var entry in targetTree)
        {
            var fullPath = ToFullPath(rootPath, entry.Key);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                session.FileSystem.CreateDirectory(directory);
            }

            var blobPath = Path.Combine(session.Context.ObjectsPath, entry.Value);
            if (!session.FileSystem.FileExists(blobPath))
            {
                throw new InvalidOperationException($"Blob '{entry.Value}' referenced by commit is missing.");
            }

            var content = await session.FileSystem.Read(blobPath);
            await session.FileSystem.WriteAtomic(fullPath, content);
        }
    }

    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    public static string ToRelativePath(string rootPath, string fullPath)
    {
        return NormalizePath(Path.GetRelativePath(rootPath, fullPath));
    }

    public static string ToFullPath(string rootPath, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var normalizedRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(rootPath));
        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, normalizedRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path escapes repository root: {relativePath}");
        }

        return fullPath;
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}

internal sealed record HeadReference(string ReferencePath, string BranchName);

internal sealed record StoredCommit(
    string CommitId,
    string? Parent,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTime CommittedAtUtc,
    Dictionary<string, string> Files);
