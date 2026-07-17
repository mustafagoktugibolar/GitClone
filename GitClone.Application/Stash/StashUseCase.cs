using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Stash;

/// <summary>
/// Shelves uncommitted changes to already-tracked files (matching plain `git stash`'s default of
/// not picking up brand-new untracked files) and resets the working tree and index to HEAD.
/// Applying a stash back reuses the same three-way merge primitive as merge/cherry-pick/rebase,
/// with base = the tree HEAD was at when the stash was pushed, so it still merges cleanly even
/// if the branch has moved on since.
/// </summary>
public sealed class StashUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<StashRequest, StashResult>
{
    public async Task<StashResult> ExecuteAsync(StashRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        return request.Action switch
        {
            StashAction.Push => await Push(session, request.Message),
            StashAction.List => await ListEntries(session),
            StashAction.Pop => await ApplyEntry(session, request.Index, pop: true),
            StashAction.Apply => await ApplyEntry(session, request.Index, pop: false),
            StashAction.Drop => await Drop(session, request.Index),
            _ => StashResult.Failed("Unknown stash action.")
        };
    }

    private static async Task<StashResult> Push(IRepositorySession session, string? message)
    {
        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var headCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var headTree = await RepositoryState.ReadCommitTree(session, headCommitId);

        var indexEntries = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var rootPath = Path.GetFullPath(session.Context.RootPath);

        await session.BlobStore.EnsureDirectory();

        // Snapshot each currently-tracked file's on-disk content. A file missing from disk is
        // treated as removed from the snapshot (not stashed as a deletion) - simple and safe.
        var currentTree = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in indexEntries)
        {
            var fullPath = RepositoryState.ToFullPath(rootPath, entry.Key);
            if (!session.FileSystem.FileExists(fullPath))
            {
                continue;
            }

            var content = await session.FileSystem.ReadBytes(fullPath);
            var hash = session.HashService.ComputeSha1(content);
            if (!session.BlobStore.Exists(hash))
            {
                await session.BlobStore.Save(hash, content);
            }

            currentTree[entry.Key] = hash;
        }

        if (TreesEqual(headTree, currentTree))
        {
            return new StashResult(true, StashOutcome.NoChanges, "No local changes to save.");
        }

        var stack = await RepositoryState.LoadStashStack(session);
        var entryMessage = string.IsNullOrWhiteSpace(message) ? "WIP" : message;
        stack.Add(new StashEntry(headCommitId, currentTree, entryMessage, DateTime.UtcNow));
        await RepositoryState.SaveStashStack(session, stack);

        // The changes are safely captured; reset the working tree and index back to HEAD.
        await RepositoryState.ApplyTreeToWorkingTree(session, headTree, indexEntries.Keys.Concat(currentTree.Keys));
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, headTree);

        return new StashResult(true, StashOutcome.Saved, $"Saved working directory state: stash@{{0}} \"{entryMessage}\".");
    }

    private static async Task<StashResult> ListEntries(IRepositorySession session)
    {
        var stack = await RepositoryState.LoadStashStack(session);
        var summaries = new List<StashEntrySummary>();
        for (var i = stack.Count - 1; i >= 0; i--)
        {
            summaries.Add(new StashEntrySummary(stack.Count - 1 - i, stack[i].Message, stack[i].BaseCommitId, stack[i].CreatedAtUtc));
        }

        return new StashResult(true, StashOutcome.Listed, "Stash entries listed.", summaries);
    }

    private static async Task<StashResult> ApplyEntry(IRepositorySession session, int requestedIndex, bool pop)
    {
        var stack = await RepositoryState.LoadStashStack(session);
        var arrayIndex = stack.Count - 1 - requestedIndex;
        if (arrayIndex < 0 || arrayIndex >= stack.Count)
        {
            return StashResult.Failed(stack.Count == 0 ? "No stash entries found." : $"No stash entry at index {requestedIndex}.");
        }

        var entry = stack[arrayIndex];
        var head = await RepositoryState.ReadHeadReference(session);

        var baseTree = await RepositoryState.ReadCommitTree(session, entry.BaseCommitId);
        var oursTree = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);
        var theirsTree = entry.Tree;

        var mergeResult = RepositoryState.MergeTrees(baseTree, oursTree, theirsTree);
        var rootPath = Path.GetFullPath(session.Context.RootPath);

        if (mergeResult.Conflicts.Count > 0)
        {
            var conflicts = await ConflictResolution.WriteConflictsAndPartialMerge(
                session, rootPath, head.BranchName, "stash", mergeResult, oursTree, theirsTree);

            return new StashResult(
                false,
                StashOutcome.Conflict,
                $"Applying stash@{{{requestedIndex}}} has {conflicts.Count} conflicting file(s). Resolve them; the stash entry was kept.",
                null,
                conflicts);
        }

        await RepositoryState.ApplyTreeToWorkingTree(session, mergeResult.Tree, oursTree.Keys.Concat(theirsTree.Keys));
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, mergeResult.Tree);

        if (pop)
        {
            stack.RemoveAt(arrayIndex);
            await RepositoryState.SaveStashStack(session, stack);
        }

        return new StashResult(
            true,
            StashOutcome.Applied,
            pop ? $"Dropped stash@{{{requestedIndex}}} after applying it." : $"Applied stash@{{{requestedIndex}}}.");
    }

    private static async Task<StashResult> Drop(IRepositorySession session, int requestedIndex)
    {
        var stack = await RepositoryState.LoadStashStack(session);
        var arrayIndex = stack.Count - 1 - requestedIndex;
        if (arrayIndex < 0 || arrayIndex >= stack.Count)
        {
            return StashResult.Failed(stack.Count == 0 ? "No stash entries found." : $"No stash entry at index {requestedIndex}.");
        }

        stack.RemoveAt(arrayIndex);
        await RepositoryState.SaveStashStack(session, stack);
        return new StashResult(true, StashOutcome.Dropped, $"Dropped stash@{{{requestedIndex}}}.");
    }

    private static bool TreesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var item in left)
        {
            if (!right.TryGetValue(item.Key, out var hash) ||
                !string.Equals(item.Value, hash, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
