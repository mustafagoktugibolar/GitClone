using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Commit;

public sealed class CommitUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<CommitRequest, CommitResult>
{
    public async Task<CommitResult> ExecuteAsync(CommitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return CommitResult.Usage("Commit message is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var currentTree = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        if (currentTree.Count == 0)
        {
            return CommitResult.NoChanges();
        }

        var head = await RepositoryState.ReadHeadReference(session);
        var branchRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var parentCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, branchRefPath);

        var parentTree = await RepositoryState.ReadCommitTree(session, parentCommitId);

        if (MapsEqual(parentTree, currentTree))
        {
            return CommitResult.NoChanges("No staged changes to commit.");
        }

        var mergeParent = await RepositoryState.ReadReferenceValue(session.FileSystem, session.Context.MergeHeadPath);
        string[] parents = (string.IsNullOrWhiteSpace(parentCommitId), string.IsNullOrWhiteSpace(mergeParent)) switch
        {
            (true, _) => [],
            (false, true) => [parentCommitId!],
            (false, false) => [parentCommitId!, mergeParent!]
        };

        var author = await RepositoryState.ReadAuthor(session);
        var committedAtUtc = DateTime.UtcNow;
        var commitId = RepositoryState.BuildCommitId(
            session.HashService,
            parents,
            request.Message.Trim(),
            committedAtUtc,
            author.Name,
            author.Email,
            currentTree);

        var commit = new StoredCommit(
            commitId,
            parents,
            request.Message.Trim(),
            author.Name,
            author.Email,
            committedAtUtc,
            currentTree);

        await RepositoryState.WriteCommit(session, commit);
        await RepositoryState.WriteReferenceValue(session.FileSystem, branchRefPath, commitId);

        if (session.FileSystem.FileExists(session.Context.MergeHeadPath))
        {
            session.FileSystem.DeleteFile(session.Context.MergeHeadPath);
        }

        return new CommitResult(
            true,
            false,
            false,
            request.Message.Trim(),
            commitId,
            head.BranchName,
            CountChangedFiles(parentTree, currentTree));
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
}
