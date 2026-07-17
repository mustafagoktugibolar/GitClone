using GitClone.Core.Abstractions;

namespace GitClone.Application.Shared;

public enum CommitMergeOutcomeKind
{
    AlreadyUpToDate,
    FastForward,
    Merged,
    Conflict
}

public sealed record CommitMergeOutcome(CommitMergeOutcomeKind Kind, string? CommitId, List<MergeConflict>? Conflicts);

/// <summary>
/// The core "merge this commit into HEAD" algorithm shared by <c>MergeUseCase</c> (which resolves
/// a branch name to a commit id first) and <c>PullUseCase</c> (which merges the commit id a fetch
/// just brought in). Keeping one implementation means fast-forward detection, conflict handling,
/// and merge-commit creation can't drift between the two callers.
/// </summary>
internal static class CommitMerging
{
    public static async Task<CommitMergeOutcome> MergeIntoCurrentBranch(
        IRepositorySession session,
        string theirsCommitId,
        string oursLabel,
        string theirsLabel,
        string mergeCommitMessage)
    {
        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var oursCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var currentIndex = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);

        if (string.Equals(oursCommitId, theirsCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return new CommitMergeOutcome(CommitMergeOutcomeKind.AlreadyUpToDate, oursCommitId, null);
        }

        if (string.IsNullOrWhiteSpace(oursCommitId))
        {
            return await FastForward(session, headRefPath, theirsCommitId, currentIndex.Keys);
        }

        var mergeBase = await RepositoryState.FindMergeBase(session, oursCommitId, theirsCommitId);

        if (string.Equals(mergeBase, theirsCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return new CommitMergeOutcome(CommitMergeOutcomeKind.AlreadyUpToDate, oursCommitId, null);
        }

        if (string.Equals(mergeBase, oursCommitId, StringComparison.OrdinalIgnoreCase))
        {
            return await FastForward(session, headRefPath, theirsCommitId, currentIndex.Keys);
        }

        var baseTree = await RepositoryState.ReadCommitTree(session, mergeBase);
        var oursTree = await RepositoryState.ReadCommitTree(session, oursCommitId);
        var theirsTree = await RepositoryState.ReadCommitTree(session, theirsCommitId);

        var mergeResult = RepositoryState.MergeTrees(baseTree, oursTree, theirsTree);
        var rootPath = Path.GetFullPath(session.Context.RootPath);

        if (mergeResult.Conflicts.Count > 0)
        {
            var conflicts = await ConflictResolution.WriteConflictsAndPartialMerge(
                session, rootPath, oursLabel, theirsLabel, mergeResult, oursTree, theirsTree);

            // MERGE_HEAD lets a follow-up `commit` produce a real two-parent merge commit once
            // the user resolves the conflicts, instead of losing the merge lineage.
            await session.FileSystem.WriteAtomic(session.Context.MergeHeadPath, theirsCommitId);

            return new CommitMergeOutcome(CommitMergeOutcomeKind.Conflict, null, conflicts);
        }

        await RepositoryState.ApplyTreeToWorkingTree(session, mergeResult.Tree, oursTree.Keys.Concat(theirsTree.Keys));
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, mergeResult.Tree);

        var author = await RepositoryState.ReadAuthor(session);
        var committedAtUtc = DateTime.UtcNow;
        string[] parents = [oursCommitId, theirsCommitId];
        var commitId = RepositoryState.BuildCommitId(
            session.HashService, parents, mergeCommitMessage, committedAtUtc, author.Name, author.Email, mergeResult.Tree);

        var commit = new StoredCommit(
            commitId, parents, mergeCommitMessage, author.Name, author.Email, committedAtUtc, mergeResult.Tree);
        await RepositoryState.WriteCommit(session, commit);
        await RepositoryState.WriteReferenceValue(session.FileSystem, headRefPath, commitId);

        return new CommitMergeOutcome(CommitMergeOutcomeKind.Merged, commitId, null);
    }

    private static async Task<CommitMergeOutcome> FastForward(
        IRepositorySession session,
        string headRefPath,
        string theirsCommitId,
        IEnumerable<string> currentIndexPaths)
    {
        var theirsTree = await RepositoryState.ReadCommitTree(session, theirsCommitId);
        await RepositoryState.ApplyTreeToWorkingTree(session, theirsTree, currentIndexPaths);
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, theirsTree);
        await RepositoryState.WriteReferenceValue(session.FileSystem, headRefPath, theirsCommitId);

        return new CommitMergeOutcome(CommitMergeOutcomeKind.FastForward, theirsCommitId, null);
    }
}
