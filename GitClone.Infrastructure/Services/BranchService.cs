using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Services;

public class BranchService(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IBranchService
{
    private string RepositoryPath => repositoryContext.IlosPath;

    public async Task EnsureCreated()
    {
        if (!Directory.Exists(RepositoryPath)) 
            return;

        if (Directory.Exists(repositoryContext.HeadsPath)) 
            return;
        
        Directory.CreateDirectory(repositoryContext.HeadsPath);
        await CreateBranch("master");
        await WriteHead("master");
    }

    public async Task WriteHead(string branchName)
    {
        await fileSystem.WriteAtomic(repositoryContext.HEADPath, $"ref: refs/heads/{branchName}");
    }

    public async Task<(string fullPath, string branchName)> ReadHead()
    {
        var fileText = await File.ReadAllTextAsync(repositoryContext.HEADPath);
        var trimmed = fileText.Trim();
        var parts = trimmed.Split(':');
        var refPart = parts.Length > 1 ? parts[1].Trim() : trimmed;
        var branchName = refPart.Split('/').Last();
        return (refPart, branchName);
    }

    public async Task CreateBranch(string branchName)
    {
        if (!Directory.Exists(repositoryContext.HeadsPath)) 
            return;
        await fileSystem.WriteAtomic(Path.Combine(repositoryContext.HeadsPath, branchName), "");
    }

    public async Task DeleteBranch(string branchName)
    {
        var branchPath = Path.Combine(repositoryContext.HeadsPath, branchName);
        if (!File.Exists(branchPath)) 
            return;

        var head = await ReadHead();
        if (head.branchName.Equals(branchName))
        {
            return;
        }

        File.Delete(branchPath);
    }

    public async Task RenameBranch(string branchName, string newBranchName)
    {
        var branchPath = Path.Combine(repositoryContext.HeadsPath, branchName);
        if(!File.Exists(branchPath))
            return;
        var file = new FileInfo(branchPath);
        file.MoveTo(Path.Combine(repositoryContext.HeadsPath, newBranchName));
        
        var head = await ReadHead();
        if (head.branchName.Equals(branchName))
        {
            await WriteHead(newBranchName);
        }
    }

    public async Task ListBranches()
    {
        var directory = new DirectoryInfo(repositoryContext.HeadsPath);
        if (!directory.Exists) 
            return;
        
        await Task.CompletedTask;
    }
}
