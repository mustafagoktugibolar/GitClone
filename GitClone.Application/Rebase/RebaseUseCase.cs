using GitClone.Application.Merge;
using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Rebase;

/// <summary>
/// Replays the commits unique to the current branch onto the tip of another branch, one at a
/// time, using the same three-way merge primitive <see cref="MergeUseCase"/> uses for each
/// individual commit. This is deliberately all-or-nothing: the whole sequence is computed in
/// memory first, and nothing is written to disk (no new commits, no ref changes, no working
/// tree changes) unless every commit replays cleanly. If any commit conflicts, the rebase is
/// aborted with no partial state - there's no `--continue`/`--abort`/rebase-in-progress
/// machinery here, so a conflicting rebase asks the user to resolve it via `merge` instead.
/// </summary>
public sealed class RebaseUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<RebaseRequest, RebaseResult>
{
    public async Task<RebaseResult> ExecuteAsync(RebaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OntoBranch))
        {
            return RebaseResult.Failed("A branch name is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var head = await RepositoryState.ReadHeadReference(session);
        if (string.Equals(head.BranchName, request.OntoBranch, StringComparison.Ordinal))
        {
            return RebaseResult.Failed("Cannot rebase a branch onto itself.");
        }

        var ontoRefPath = RepositoryState.ResolveBranchRefPath(session.Context, request.OntoBranch);
        if (!session.FileSystem.FileExists(ontoRefPath))
        {
            return RebaseResult.Failed($"Branch not found: {request.OntoBranch}");
        }

        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var currentCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var ontoCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, ontoRefPath);

        if (string.IsNullOrWhiteSpace(currentCommitId))
        {
            return RebaseResult.Failed("Nothing to rebase: current branch has no commits.");
        }

        if (string.IsNullOrWhiteSpace(ontoCommitId))
        {
            return new RebaseResult(true, RebaseOutcome.AlreadyUpToDate, $"Branch '{request.OntoBranch}' has no commits; nothing to rebase onto.");
        }

        if (string.Equals(currentCommitId, ontoCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return new RebaseResult(true, RebaseOutcome.AlreadyUpToDate, "Already up to date.");
        }

        var mergeBase = await RepositoryState.FindMergeBase(session, currentCommitId, ontoCommitId);
        if (string.Equals(mergeBase, ontoCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return new RebaseResult(true, RebaseOutcome.AlreadyUpToDate, $"Current branch is already based on '{request.OntoBranch}'.");
        }

        var commitsToReplay = await CollectCommitsSinceBase(session, currentCommitId, mergeBase);
        if (commitsToReplay.Count == 0)
        {
            return new RebaseResult(true, RebaseOutcome.AlreadyUpToDate, "Nothing to replay.");
        }

        var newParentId = ontoCommitId;
        var currentTree = await RepositoryState.ReadCommitTree(session, ontoCommitId);
        var pendingCommits = new List<StoredCommit>();

        foreach (var original in commitsToReplay)
        {
            var originalParentId = original.Parents.Length > 0 ? original.Parents[0] : null;
            var baseTree = await RepositoryState.ReadCommitTree(session, originalParentId);
            var theirsTree = new Dictionary<string, string>(original.Files, StringComparer.OrdinalIgnoreCase);

            var step = RepositoryState.MergeTrees(baseTree, currentTree, theirsTree);
            if (step.Conflicts.Count > 0)
            {
                return new RebaseResult(
                    false,
                    RebaseOutcome.Conflict,
                    $"Rebase stopped: commit '{original.CommitId}' (\"{original.Message}\") conflicts with '{request.OntoBranch}'. " +
                    "No changes were made. Resolve this with `merge` instead.",
                    pendingCommits.Count,
                    step.Conflicts.Select(path => new MergeConflict(path, false)).ToList());
            }

            currentTree = step.Tree;
            var committedAtUtc = DateTime.UtcNow;
            string[] newParents = [newParentId];
            var commitId = RepositoryState.BuildCommitId(
                session.HashService, newParents, original.Message, committedAtUtc, original.AuthorName, original.AuthorEmail, currentTree);

            var newCommit = new StoredCommit(
                commitId, newParents, original.Message, original.AuthorName, original.AuthorEmail, committedAtUtc, currentTree);
            pendingCommits.Add(newCommit);
            newParentId = commitId;
        }

        // Every commit replayed cleanly - only now do we actually touch disk.
        foreach (var commit in pendingCommits)
        {
            await RepositoryState.WriteCommit(session, commit);
        }

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        await RepositoryState.ApplyTreeToWorkingTree(session, currentTree, indexEntries.Keys);
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, currentTree);
        await RepositoryState.WriteReferenceValue(session.FileSystem, headRefPath, newParentId);

        return new RebaseResult(
            true, RebaseOutcome.Rebased, $"Rebased {pendingCommits.Count} commit(s) onto '{request.OntoBranch}'.", pendingCommits.Count);
    }

    private static async Task<List<StoredCommit>> CollectCommitsSinceBase(
        IRepositorySession session, string currentCommitId, string? mergeBase)
    {
        var result = new List<StoredCommit>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = currentCommitId;

        while (!string.IsNullOrWhiteSpace(current) && !string.Equals(current, mergeBase, StringComparison.OrdinalIgnoreCase))
        {
            if (!visited.Add(current))
            {
                break;
            }

            var commit = await RepositoryState.ReadCommit(session, current);
            if (commit is null)
            {
                throw new InvalidOperationException($"Commit '{current}' is missing.");
            }

            result.Add(commit);
            current = commit.Parents.Length > 0 ? commit.Parents[0] : null;
        }

        result.Reverse();
        return result;
    }
}
