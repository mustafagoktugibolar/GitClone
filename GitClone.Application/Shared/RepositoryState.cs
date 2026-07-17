using System.Text;
using System.Text.Json;
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

            // Split on the last space - paths may themselves contain spaces, but the hash
            // (the last token) never does, so the rightmost space is the only delimiter
            // position that's unambiguous.
            var separator = line.LastIndexOf(' ');
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

    public static string ResolveBranchRefPath(IRepositoryContext context, string branchName)
    {
        return Path.Combine(context.HeadsPath, branchName);
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

    public static async Task WriteCommit(IRepositorySession session, StoredCommit commit)
    {
        session.FileSystem.CreateDirectory(session.Context.CommitsPath);
        var commitPath = Path.Combine(session.Context.CommitsPath, $"{commit.CommitId}.json");
        var payload = JsonSerializer.Serialize(commit, JsonOptions);
        await session.FileSystem.WriteAtomic(commitPath, payload);
    }

    public static string BuildCommitId(
        IHashService hashService,
        IReadOnlyList<string> parents,
        string message,
        DateTime committedAtUtc,
        string authorName,
        string authorEmail,
        IReadOnlyDictionary<string, string> tree)
    {
        var builder = new StringBuilder();
        builder.Append("parents=").Append(string.Join(',', parents)).Append('\n');
        builder.Append("author=").Append(authorName).Append('<').Append(authorEmail).Append('>').Append('\n');
        builder.Append("date=").Append(committedAtUtc.ToString("O")).Append('\n');
        builder.Append("message=").Append(message).Append('\n');

        foreach (var entry in tree.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            builder.Append(entry.Key).Append(' ').Append(entry.Value).Append('\n');
        }

        return hashService.ComputeSha1(builder.ToString());
    }

    public static async Task<CommitAuthor> ReadAuthor(IRepositorySession session)
    {
        if (!session.FileSystem.FileExists(session.Context.LocalConfigPath))
        {
            return new CommitAuthor("unknown", "unknown@local");
        }

        var json = await session.FileSystem.Read(session.Context.LocalConfigPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CommitAuthor("unknown", "unknown@local");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("activeUser", out var activeUserElement))
        {
            return new CommitAuthor("unknown", "unknown@local");
        }

        var activeUser = activeUserElement.GetString();
        if (string.IsNullOrWhiteSpace(activeUser))
        {
            return new CommitAuthor("unknown", "unknown@local");
        }

        if (!root.TryGetProperty("configs", out var users) || users.ValueKind != JsonValueKind.Array)
        {
            return new CommitAuthor("unknown", activeUser);
        }

        foreach (var user in users.EnumerateArray())
        {
            var email = user.TryGetProperty("mail", out var emailProp) ? emailProp.GetString() : null;
            if (!string.Equals(email, activeUser, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var username = user.TryGetProperty("username", out var userNameProp)
                ? userNameProp.GetString()
                : null;

            return new CommitAuthor(
                string.IsNullOrWhiteSpace(username) ? "unknown" : username!,
                activeUser);
        }

        return new CommitAuthor("unknown", activeUser);
    }

    public static async Task<Dictionary<string, string>> ReadCommitTree(IRepositorySession session, string? commitId)
    {
        if (string.IsNullOrWhiteSpace(commitId))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var commit = await ReadCommit(session, commitId);
        if (commit is null)
        {
            throw new InvalidOperationException($"Commit '{commitId}' is missing.");
        }

        return new Dictionary<string, string>(commit.Files, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Collects every commit reachable from <paramref name="commitId"/> by walking all parent
    /// links (not just the first), so merge commits are traversed correctly.
    /// </summary>
    public static async Task<HashSet<string>> GetAncestors(IRepositorySession session, string commitId)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(commitId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            var commit = await ReadCommit(session, current);
            if (commit is null)
            {
                continue;
            }

            foreach (var parent in commit.Parents)
            {
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    queue.Enqueue(parent);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Finds a common ancestor of two commits by walking <paramref name="commitB"/>'s ancestry
    /// (breadth-first, closest first) and returning the first commit also reachable from
    /// <paramref name="commitA"/>. In histories with multiple merge commits there can be more
    /// than one valid merge base; this returns one reasonable candidate, not necessarily the
    /// unique "best" one a full LCA algorithm would pick.
    /// </summary>
    public static async Task<string?> FindMergeBase(IRepositorySession session, string commitA, string commitB)
    {
        var ancestorsOfA = await GetAncestors(session, commitA);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(commitB);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (ancestorsOfA.Contains(current))
            {
                return current;
            }

            var commit = await ReadCommit(session, current);
            if (commit is null)
            {
                continue;
            }

            foreach (var parent in commit.Parents)
            {
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    queue.Enqueue(parent);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Three-way merges two trees against their common base. A path is a conflict only when
    /// both sides changed it (from the base) to different results - independent changes to
    /// different paths, or a change on only one side, merge cleanly.
    /// </summary>
    public static TreeMergeResult MergeTrees(
        IReadOnlyDictionary<string, string> baseTree,
        IReadOnlyDictionary<string, string> oursTree,
        IReadOnlyDictionary<string, string> theirsTree)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<string>();

        var allPaths = baseTree.Keys
            .Concat(oursTree.Keys)
            .Concat(theirsTree.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var path in allPaths)
        {
            baseTree.TryGetValue(path, out var baseHash);
            oursTree.TryGetValue(path, out var oursHash);
            theirsTree.TryGetValue(path, out var theirsHash);

            var oursChanged = !string.Equals(baseHash, oursHash, StringComparison.OrdinalIgnoreCase);
            var theirsChanged = !string.Equals(baseHash, theirsHash, StringComparison.OrdinalIgnoreCase);

            if (!oursChanged && !theirsChanged)
            {
                if (baseHash is not null)
                {
                    merged[path] = baseHash;
                }
                continue;
            }

            if (oursChanged && !theirsChanged)
            {
                if (oursHash is not null)
                {
                    merged[path] = oursHash;
                }
                continue;
            }

            if (!oursChanged && theirsChanged)
            {
                if (theirsHash is not null)
                {
                    merged[path] = theirsHash;
                }
                continue;
            }

            // Both sides changed the path. If they landed on the same result (including both
            // deleting it), that's not a conflict.
            if (string.Equals(oursHash, theirsHash, StringComparison.OrdinalIgnoreCase))
            {
                if (oursHash is not null)
                {
                    merged[path] = oursHash;
                }
                continue;
            }

            conflicts.Add(path);
        }

        return new TreeMergeResult(merged, conflicts);
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
            await WriteBlobToWorkingTree(session, rootPath, entry.Key, entry.Value);
        }
    }

    public static async Task WriteBlobToWorkingTree(IRepositorySession session, string rootPath, string relativePath, string blobHash)
    {
        var fullPath = ToFullPath(rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            session.FileSystem.CreateDirectory(directory);
        }

        var blobPath = Path.Combine(session.Context.ObjectsPath, blobHash);
        if (!session.FileSystem.FileExists(blobPath))
        {
            throw new InvalidOperationException($"Blob '{blobHash}' referenced by commit is missing.");
        }

        var content = await session.FileSystem.ReadBytes(blobPath);
        await session.FileSystem.WriteAtomicBytes(fullPath, content);
    }

    public static async Task<byte[]> ReadBlob(IRepositorySession session, string blobHash)
    {
        var blobPath = Path.Combine(session.Context.ObjectsPath, blobHash);
        return await session.FileSystem.ReadBytes(blobPath);
    }

    /// <summary>
    /// Copies every commit and blob reachable from <paramref name="commitId"/> from
    /// <paramref name="source"/> into <paramref name="target"/> that <paramref name="target"/>
    /// doesn't already have. Used by fetch, push, and local-path clone - all three are really
    /// "copy this commit graph between two .ilos repositories", just in different directions.
    /// Stops walking a branch of history as soon as it hits a commit the target already has,
    /// since everything below that commit must already have been copied too.
    /// </summary>
    public static async Task CopyCommitGraph(IRepositorySession source, IRepositorySession target, string commitId)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(commitId);

        target.FileSystem.CreateDirectory(target.Context.ObjectsPath);
        target.FileSystem.CreateDirectory(target.Context.CommitsPath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            var targetCommitPath = Path.Combine(target.Context.CommitsPath, $"{current}.json");
            if (target.FileSystem.FileExists(targetCommitPath))
            {
                continue;
            }

            var commit = await ReadCommit(source, current);
            if (commit is null)
            {
                continue;
            }

            foreach (var hash in commit.Files.Values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var targetBlobPath = Path.Combine(target.Context.ObjectsPath, hash);
                if (target.FileSystem.FileExists(targetBlobPath))
                {
                    continue;
                }

                var content = await ReadBlob(source, hash);
                await target.FileSystem.WriteAtomicBytes(targetBlobPath, content);
            }

            var sourceCommitPath = Path.Combine(source.Context.CommitsPath, $"{current}.json");
            var commitJson = await source.FileSystem.Read(sourceCommitPath);
            await target.FileSystem.WriteAtomic(targetCommitPath, commitJson);

            foreach (var parent in commit.Parents)
            {
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    queue.Enqueue(parent);
                }
            }
        }
    }

    public static async Task<Dictionary<string, string>> LoadRemotes(IRepositorySession session)
    {
        if (!session.FileSystem.FileExists(session.Context.RemotesConfigPath))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var json = await session.FileSystem.Read(session.Context.RemotesConfigPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
               ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static Task SaveRemotes(IRepositorySession session, Dictionary<string, string> remotes)
    {
        var json = JsonSerializer.Serialize(remotes, JsonOptions);
        return session.FileSystem.WriteAtomic(session.Context.RemotesConfigPath, json);
    }

    public static async Task<(bool Found, string? Path, string? Error)> ResolveRemote(
        IRepositorySession session, IRepositorySessionFactory sessionFactory, string remoteName)
    {
        var remotes = await LoadRemotes(session);
        if (!remotes.TryGetValue(remoteName, out var path))
        {
            return (false, null, $"Remote not found: {remoteName}");
        }

        var remoteSession = sessionFactory.CreateForPath(path);
        if (!remoteSession.FileSystem.DirectoryExists(remoteSession.Context.IlosPath))
        {
            return (false, null, $"Remote '{remoteName}' path is not a valid repository: {path}");
        }

        return (true, path, null);
    }

    public static async Task<List<StashEntry>> LoadStashStack(IRepositorySession session)
    {
        if (!session.FileSystem.FileExists(session.Context.StashPath))
        {
            return [];
        }

        var json = await session.FileSystem.Read(session.Context.StashPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<StashEntry>>(json, JsonOptions) ?? [];
    }

    public static Task SaveStashStack(IRepositorySession session, List<StashEntry> stack)
    {
        var json = JsonSerializer.Serialize(stack, JsonOptions);
        return session.FileSystem.WriteAtomic(session.Context.StashPath, json);
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

internal sealed record CommitAuthor(string Name, string Email);

internal sealed record TreeMergeResult(Dictionary<string, string> Tree, List<string> Conflicts);

internal sealed record StoredCommit(
    string CommitId,
    string[] Parents,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTime CommittedAtUtc,
    Dictionary<string, string> Files);

internal sealed record StashEntry(
    string? BaseCommitId,
    Dictionary<string, string> Tree,
    string Message,
    DateTime CreatedAtUtc);
