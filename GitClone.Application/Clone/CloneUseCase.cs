using GitClone.Application.Init;
using GitClone.Application.Shared;
using GitClone.Core.Abstractions;

namespace GitClone.Application.Clone;

public sealed class CloneUseCase(
    InitUseCase initUseCase,
    ICloneExecutor cloneExecutor,
    IRepositorySessionFactory repositorySessionFactory) : IUseCase<CloneRequest, CloneResult>
{
    public async Task<CloneResult> ExecuteAsync(CloneRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentException("url is required.");
        }

        if (IsHttpUrl(request.Url))
        {
            // GitHub-over-HTTP clone: downloads a ZIP snapshot of one branch. There is no git
            // wire protocol implementation here, so this is a snapshot only - no commit history,
            // no other branches. Local-path sources (below) get a real clone with full history.
            var clone = await cloneExecutor.ExecuteAsync(
                new CloneExecutionRequest(request.Url, request.WorkingDirectory, request.ProjectName, request.Branch, request.Location));

            await initUseCase.ExecuteAsync(new InitRequest(clone.TargetDirectory));
            return new CloneResult(clone.RepositoryName, clone.BranchName, clone.TargetDirectory);
        }

        return await CloneLocalRepository(request);
    }

    private async Task<CloneResult> CloneLocalRepository(CloneRequest request)
    {
        var sourcePath = Path.GetFullPath(request.Url);
        var sourceSession = repositorySessionFactory.CreateForPath(sourcePath);
        if (!sourceSession.FileSystem.DirectoryExists(sourceSession.Context.IlosPath))
        {
            throw new InvalidOperationException($"Not a repository: {sourcePath}");
        }

        var repositoryName = string.IsNullOrWhiteSpace(request.ProjectName)
            ? Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : request.ProjectName;
        var targetDirectory = string.IsNullOrWhiteSpace(request.Location)
            ? Path.GetFullPath(Path.Combine(request.WorkingDirectory, repositoryName))
            : Path.GetFullPath(request.Location);

        // forInit avoids RepoLocator walking up to some ancestor repo when targetDirectory
        // doesn't exist yet or isn't a repo itself - it must resolve exactly to targetDirectory.
        var preflightSession = repositorySessionFactory.CreateForPath(targetDirectory, forInit: true);
        if (preflightSession.FileSystem.DirectoryExists(preflightSession.Context.IlosPath))
        {
            throw new InvalidOperationException($"Destination already contains a repository: {targetDirectory}");
        }

        await initUseCase.ExecuteAsync(new InitRequest(targetDirectory));
        var targetSession = repositorySessionFactory.CreateForPath(targetDirectory);

        var sourceHead = await RepositoryState.ReadHeadReference(sourceSession);
        var checkoutBranch = string.IsNullOrWhiteSpace(request.Branch) ? sourceHead.BranchName : request.Branch;

        var sourceBranches = sourceSession.FileSystem.DirectoryExists(sourceSession.Context.HeadsPath)
            ? sourceSession.FileSystem.GetFiles(sourceSession.Context.HeadsPath)
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Select(name => name!)
                .ToList()
            : [];

        var copiedAnyBranch = false;
        foreach (var branch in sourceBranches)
        {
            var sourceBranchRefPath = RepositoryState.ResolveBranchRefPath(sourceSession.Context, branch);
            var commitId = await RepositoryState.ReadReferenceValue(sourceSession.FileSystem, sourceBranchRefPath);
            if (string.IsNullOrWhiteSpace(commitId))
            {
                continue;
            }

            await RepositoryState.CopyCommitGraph(sourceSession, targetSession, commitId);

            var targetBranchRefPath = RepositoryState.ResolveBranchRefPath(targetSession.Context, branch);
            await RepositoryState.WriteReferenceValue(targetSession.FileSystem, targetBranchRefPath, commitId);
            copiedAnyBranch = true;
        }

        // InitUseCase always creates an empty "master" branch; drop it if the source didn't
        // actually have one, so cloning a repo whose default branch is e.g. "main" doesn't leave
        // a stray empty branch behind.
        if (!sourceBranches.Contains("master", StringComparer.Ordinal))
        {
            var staleMasterPath = RepositoryState.ResolveBranchRefPath(targetSession.Context, "master");
            if (targetSession.FileSystem.FileExists(staleMasterPath))
            {
                targetSession.FileSystem.DeleteFile(staleMasterPath);
            }
        }

        var checkoutRefPath = RepositoryState.ResolveBranchRefPath(targetSession.Context, checkoutBranch);
        if (!targetSession.FileSystem.FileExists(checkoutRefPath))
        {
            throw new InvalidOperationException($"Branch not found in source repository: {checkoutBranch}");
        }

        await targetSession.BranchService.WriteHead(checkoutBranch);

        if (copiedAnyBranch)
        {
            var checkoutCommitId = await RepositoryState.ReadReferenceValue(targetSession.FileSystem, checkoutRefPath);
            var tree = await RepositoryState.ReadCommitTree(targetSession, checkoutCommitId);
            await RepositoryState.ApplyTreeToWorkingTree(targetSession, tree, []);
            await RepositoryState.SaveIndexEntries(targetSession.FileSystem, targetSession.Context.IndexPath, tree);
        }

        // Match `git clone`'s convention of automatically wiring up the source as "origin", so
        // fetch/push work immediately after cloning.
        var remotes = await RepositoryState.LoadRemotes(targetSession);
        remotes["origin"] = sourcePath;
        await RepositoryState.SaveRemotes(targetSession, remotes);

        return new CloneResult(repositoryName!, checkoutBranch, targetDirectory);
    }

    private static bool IsHttpUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
