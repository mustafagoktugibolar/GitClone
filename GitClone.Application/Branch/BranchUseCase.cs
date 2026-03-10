using GitClone.Core.Abstractions;

namespace GitClone.Application.Branch;

public sealed class BranchUseCase(IRepositorySessionFactory repositorySessionFactory) : IUseCase<BranchRequest, BranchResult>
{
    public async Task<BranchResult> ExecuteAsync(BranchRequest request)
    {
        var session = repositorySessionFactory.CreateForPath(request.WorkingDirectory);
        var args = request.Args;

        if (args.Length == 1)
        {
            return await ListBranches(session);
        }

        if (args.Length == 2 &&
            (args[1].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
             args[1].Equals("help", StringComparison.OrdinalIgnoreCase)))
        {
            return Usage();
        }

        if (args.Length == 2)
        {
            return await CreateBranch(session, args[1]);
        }

        if (args.Length == 3 &&
            (args[1].Equals("-d", StringComparison.OrdinalIgnoreCase) ||
             args[1].Equals("--delete", StringComparison.OrdinalIgnoreCase)))
        {
            return await DeleteBranch(session, args[2]);
        }

        if (args.Length == 4 && args[1].Equals("-m", StringComparison.OrdinalIgnoreCase))
        {
            return await RenameBranch(session, args[2], args[3]);
        }

        return Usage("Invalid branch arguments.");
    }

    private static async Task<BranchResult> ListBranches(IRepositorySession session)
    {
        var branches = session.FileSystem.DirectoryExists(session.Context.HeadsPath)
            ? session.FileSystem.GetFiles(session.Context.HeadsPath).Select(Path.GetFileName).Where(name => name != null).Select(name => name!).OrderBy(name => name, StringComparer.Ordinal).ToList()
            : [];

        var headBranch = await ReadHeadBranch(session);
        return new BranchResult(BranchOperation.List, true, "Branches listed.", headBranch, branches);
    }

    private static async Task<BranchResult> CreateBranch(IRepositorySession session, string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return Usage("Branch name is required.");
        }

        var branchPath = Path.Combine(session.Context.HeadsPath, branchName);
        if (session.FileSystem.FileExists(branchPath))
        {
            return new BranchResult(BranchOperation.Create, false, $"Branch already exists: {branchName}");
        }

        session.FileSystem.CreateDirectory(session.Context.HeadsPath);
        var headBranch = await ReadHeadBranch(session);
        var initialCommitId = string.Empty;

        if (!string.IsNullOrWhiteSpace(headBranch))
        {
            var headBranchPath = Path.Combine(session.Context.HeadsPath, headBranch);
            if (session.FileSystem.FileExists(headBranchPath))
            {
                initialCommitId = (await session.FileSystem.Read(headBranchPath)).Trim();
            }
        }

        await session.FileSystem.WriteAtomic(branchPath, initialCommitId);
        return new BranchResult(BranchOperation.Create, true, $"Created branch '{branchName}'.");
    }

    private static async Task<BranchResult> DeleteBranch(IRepositorySession session, string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            return Usage("Branch name is required.");
        }

        var branchPath = Path.Combine(session.Context.HeadsPath, branchName);
        if (!session.FileSystem.FileExists(branchPath))
        {
            return new BranchResult(BranchOperation.Delete, false, $"Branch not found: {branchName}");
        }

        var headBranch = await ReadHeadBranch(session);
        if (string.Equals(headBranch, branchName, StringComparison.Ordinal))
        {
            return new BranchResult(BranchOperation.Delete, false, "Head branch can't be deleted!");
        }

        session.FileSystem.DeleteFile(branchPath);
        return new BranchResult(BranchOperation.Delete, true, $"Deleted branch '{branchName}'.");
    }

    private static async Task<BranchResult> RenameBranch(IRepositorySession session, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        {
            return Usage("Old and new branch names are required.");
        }

        var oldPath = Path.Combine(session.Context.HeadsPath, oldName);
        var newPath = Path.Combine(session.Context.HeadsPath, newName);

        if (!session.FileSystem.FileExists(oldPath))
        {
            return new BranchResult(BranchOperation.Rename, false, $"Branch not found: {oldName}");
        }

        if (session.FileSystem.FileExists(newPath))
        {
            return new BranchResult(BranchOperation.Rename, false, $"Branch already exists: {newName}");
        }

        session.FileSystem.MoveFile(oldPath, newPath);

        var headBranch = await ReadHeadBranch(session);
        if (string.Equals(headBranch, oldName, StringComparison.Ordinal))
        {
            await session.FileSystem.WriteAtomic(session.Context.HEADPath, $"ref: refs/heads/{newName}");
        }

        return new BranchResult(BranchOperation.Rename, true, $"Renamed branch '{oldName}' to '{newName}'.");
    }

    private static async Task<string?> ReadHeadBranch(IRepositorySession session)
    {
        if (!session.FileSystem.FileExists(session.Context.HEADPath))
        {
            return null;
        }

        var text = await session.FileSystem.Read(session.Context.HEADPath);
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var parts = trimmed.Split(':', 2);
        var reference = parts.Length == 2 ? parts[1].Trim() : trimmed;
        return reference.Split('/').LastOrDefault();
    }

    private static BranchResult Usage(string? error = null)
    {
        var message = string.IsNullOrWhiteSpace(error)
            ? "Usage: ilos branch [<name> | -d <name> | -m <old> <new>]"
            : $"{error} Usage: ilos branch [<name> | -d <name> | -m <old> <new>]";
        return new BranchResult(BranchOperation.Usage, false, message);
    }
}
