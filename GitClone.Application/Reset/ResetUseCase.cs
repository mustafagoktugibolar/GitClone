using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Reset;

public sealed class ResetUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<ResetRequest, ResetResult>
{
    public async Task<ResetResult> ExecuteAsync(ResetRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        await session.IndexManager.EnsureCreated();

        var head = await RepositoryState.ReadHeadReference(session);
        var headRefPath = RepositoryState.ResolveReferencePath(session.Context, head.ReferencePath);
        var currentHeadCommit = await RepositoryState.ReadReferenceValue(session.FileSystem, headRefPath);
        var targetCommit = await ResolveTargetCommit(session, currentHeadCommit, request.TargetSpec);
        var targetTree = await RepositoryState.ReadCommitTree(session, targetCommit);

        var currentIndex = await RepositoryState.LoadIndexEntries(session.FileSystem, session.Context.IndexPath);

        await RepositoryState.WriteReferenceValue(session.FileSystem, headRefPath, targetCommit);

        if (request.Mode is ResetMode.Mixed or ResetMode.Hard)
        {
            await RepositoryState.SaveIndexEntries(session.FileSystem, session.Context.IndexPath, targetTree);
        }

        if (request.Mode == ResetMode.Hard)
        {
            await RepositoryState.ApplyTreeToWorkingTree(session, targetTree, currentIndex.Keys);
        }

        var modeText = request.Mode.ToString().ToLowerInvariant();
        return new ResetResult(true, false, $"Reset {modeText} to {(targetCommit ?? "empty state")}.", targetCommit);
    }

    private static async Task<string?> ResolveTargetCommit(
        IRepositorySession session,
        string? currentHeadCommit,
        string? targetSpec)
    {
        if (string.IsNullOrWhiteSpace(targetSpec) || targetSpec.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return currentHeadCommit;
        }

        if (targetSpec.StartsWith("HEAD~", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(targetSpec[5..], out var distance) || distance < 0)
            {
                throw new ArgumentException($"Invalid revision: {targetSpec}");
            }

            var commitId = currentHeadCommit;
            for (var i = 0; i < distance; i++)
            {
                if (string.IsNullOrWhiteSpace(commitId))
                {
                    throw new InvalidOperationException($"Revision out of range: {targetSpec}");
                }

                var commit = await RepositoryState.ReadCommit(session, commitId);
                if (commit == null)
                {
                    throw new InvalidOperationException($"Commit '{commitId}' is missing.");
                }

                commitId = commit.Parent;
            }

            return commitId;
        }

        var directCommit = await RepositoryState.ReadCommit(session, targetSpec);
        if (directCommit == null)
        {
            throw new InvalidOperationException($"Unknown commit: {targetSpec}");
        }

        return directCommit.CommitId;
    }
}
