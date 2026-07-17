using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.CherryPick;

/// <summary>
/// Applies a single existing commit's changes onto the current branch as a new, single-parent
/// commit - reusing the same three-way merge primitive <c>MergeUseCase</c> and <c>RebaseUseCase</c>
/// use, with base = the target commit's original parent tree, ours = current HEAD, theirs = the
/// target commit's tree. On conflict, the same partial-auto-merge-plus-markers behavior as a
/// merge applies; resolving and running `commit` afterward yields a normal single-parent commit
/// (cherry-pick never produces merge commits), so no MERGE_HEAD bookkeeping is needed here.
/// </summary>
public sealed class CherryPickUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<CherryPickRequest, CherryPickResult>
{
    public async Task<CherryPickResult> ExecuteAsync(CherryPickRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CommitId))
        {
            return CherryPickResult.Failed("A commit id is required.");
        }

        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var target = await RepositoryState.ReadCommit(session, request.CommitId);
        if (target is null)
        {
            return CherryPickResult.Failed($"Unknown commit: {request.CommitId}");
        }

        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var oursCommitId = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);

        var originalParentId = target.Parents.Length > 0 ? target.Parents[0] : null;
        var baseTree = await RepositoryState.ReadCommitTree(session, originalParentId);
        var oursTree = await RepositoryState.ReadCommitTree(session, oursCommitId);
        var theirsTree = new Dictionary<string, string>(target.Files, StringComparer.OrdinalIgnoreCase);

        var mergeResult = RepositoryState.MergeTrees(baseTree, oursTree, theirsTree);
        var rootPath = Path.GetFullPath(session.Context.RootPath);

        if (mergeResult.Conflicts.Count > 0)
        {
            var shortId = target.CommitId[..Math.Min(7, target.CommitId.Length)];
            var conflicts = await ConflictResolution.WriteConflictsAndPartialMerge(
                session, rootPath, head.BranchName, shortId, mergeResult, oursTree, theirsTree);

            return new CherryPickResult(
                false,
                CherryPickOutcome.Conflict,
                $"Cherry-pick of '{request.CommitId}' has {conflicts.Count} conflicting file(s). Resolve them, then commit.",
                null,
                conflicts);
        }

        await RepositoryState.ApplyTreeToWorkingTree(session, mergeResult.Tree, oursTree.Keys.Concat(theirsTree.Keys));
        await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, mergeResult.Tree);

        var committedAtUtc = DateTime.UtcNow;
        string[] parents = string.IsNullOrWhiteSpace(oursCommitId) ? [] : [oursCommitId];
        var message = $"{target.Message} (cherry picked from commit {target.CommitId})";
        var commitId = RepositoryState.BuildCommitId(
            session.HashService, parents, message, committedAtUtc, target.AuthorName, target.AuthorEmail, mergeResult.Tree);

        var newCommit = new StoredCommit(
            commitId, parents, message, target.AuthorName, target.AuthorEmail, committedAtUtc, mergeResult.Tree);
        await RepositoryState.WriteCommit(session, newCommit);
        await RepositoryState.WriteReferenceValue(session.FileSystem, headRefPath, commitId);

        return new CherryPickResult(true, CherryPickOutcome.Applied, $"Applied commit {request.CommitId} as {commitId}.", commitId);
    }
}
