using System.Text;
using System.Text.Json;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Application.Commit;

public sealed class CommitUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<CommitRequest, CommitResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<CommitResult> ExecuteAsync(CommitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return CommitResult.Usage("Commit message is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();
        session.FileSystem.CreateDirectory(session.Context.CommitsPath);

        var currentTree = await LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        if (currentTree.Count == 0)
        {
            return CommitResult.NoChanges();
        }

        var head = await ReadHeadReference(session);
        var branchRefPath = ResolveReferencePath(session.Context, head.ReferencePath);
        var parentCommitId = await ReadReferenceValue(session.FileSystem, branchRefPath);

        var parentTree = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(parentCommitId))
        {
            var parentCommit = await ReadCommit(session.FileSystem, session.Context.CommitsPath, parentCommitId);
            if (parentCommit == null)
            {
                throw new InvalidOperationException($"Head commit '{parentCommitId}' is missing.");
            }

            parentTree = new Dictionary<string, string>(parentCommit.Files, StringComparer.OrdinalIgnoreCase);
        }

        if (MapsEqual(parentTree, currentTree))
        {
            return CommitResult.NoChanges("No staged changes to commit.");
        }

        var author = await ReadAuthor(session);
        var committedAtUtc = DateTime.UtcNow;
        var commitId = BuildCommitId(
            session.HashService,
            parentCommitId,
            request.Message.Trim(),
            committedAtUtc,
            author,
            currentTree);

        var commit = new StoredCommit(
            commitId,
            parentCommitId,
            request.Message.Trim(),
            author.Name,
            author.Email,
            committedAtUtc,
            currentTree);

        var commitPath = Path.Combine(session.Context.CommitsPath, $"{commitId}.json");
        var payload = JsonSerializer.Serialize(commit, JsonOptions);
        await session.FileSystem.WriteAtomic(commitPath, payload);
        await session.FileSystem.WriteAtomic(branchRefPath, commitId);

        return new CommitResult(
            true,
            false,
            false,
            request.Message.Trim(),
            commitId,
            head.BranchName,
            CountChangedFiles(parentTree, currentTree));
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

    private static async Task<HeadReference> ReadHeadReference(IRepositorySession session)
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

    private static string ResolveReferencePath(IRepositoryContext context, string referencePath)
    {
        var normalized = referencePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(context.IlosPath, normalized));
    }

    private static async Task<string?> ReadReferenceValue(IFileSystem fileSystem, string path)
    {
        if (!fileSystem.FileExists(path))
        {
            return null;
        }

        var value = (await fileSystem.Read(path)).Trim();
        return value.Length == 0 ? null : value;
    }

    private static async Task<StoredCommit?> ReadCommit(IFileSystem fileSystem, string commitsPath, string commitId)
    {
        var commitPath = Path.Combine(commitsPath, $"{commitId}.json");
        if (!fileSystem.FileExists(commitPath))
        {
            return null;
        }

        var content = await fileSystem.Read(commitPath);
        return JsonSerializer.Deserialize<StoredCommit>(content, JsonOptions);
    }

    private static async Task<CommitAuthor> ReadAuthor(IRepositorySession session)
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

    private static string BuildCommitId(
        IHashService hashService,
        string? parentCommitId,
        string message,
        DateTime committedAtUtc,
        CommitAuthor author,
        IReadOnlyDictionary<string, string> tree)
    {
        var builder = new StringBuilder();
        builder.Append("parent=").Append(parentCommitId ?? string.Empty).Append('\n');
        builder.Append("author=").Append(author.Name).Append('<').Append(author.Email).Append('>').Append('\n');
        builder.Append("date=").Append(committedAtUtc.ToString("O")).Append('\n');
        builder.Append("message=").Append(message).Append('\n');

        foreach (var entry in tree.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            builder.Append(entry.Key).Append(' ').Append(entry.Value).Append('\n');
        }

        return hashService.ComputeSha1(builder.ToString());
    }

    private static int CountChangedFiles(
        IReadOnlyDictionary<string, string> previous,
        IReadOnlyDictionary<string, string> current)
    {
        var allPaths = previous.Keys
            .Concat(current.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var changed = 0;
        foreach (var path in allPaths)
        {
            var oldHash = previous.GetValueOrDefault(path);
            var newHash = current.GetValueOrDefault(path);
            if (!string.Equals(oldHash, newHash, StringComparison.OrdinalIgnoreCase))
            {
                changed++;
            }
        }

        return changed;
    }

    private static bool MapsEqual(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var item in left)
        {
            if (!right.TryGetValue(item.Key, out var hash))
            {
                return false;
            }

            if (!string.Equals(item.Value, hash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed record HeadReference(string ReferencePath, string BranchName);

    private sealed record CommitAuthor(string Name, string Email);

    private sealed record StoredCommit(
        string CommitId,
        string? Parent,
        string Message,
        string AuthorName,
        string AuthorEmail,
        DateTime CommittedAtUtc,
        Dictionary<string, string> Files);
}
