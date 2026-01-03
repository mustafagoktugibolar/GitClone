using GitClone.Interfaces;

namespace GitClone.Services;

public class BranchService(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IBranchService
{
    private string RepositoryPath => repositoryContext.IlosPath;
    public async Task EnsureCreated()
    {
        if (!Directory.Exists(RepositoryPath)) 
            return;
        var headsPath = repositoryContext.HeadsPath;
        if (Directory.Exists(headsPath)) 
            return;
        
        Directory.CreateDirectory(headsPath);
        await CreateBranch("master");
        await WriteHead("master");
    }

    public async Task WriteHead(string branchName)
    {
        try
        {
            await fileSystem.WriteAtomic(repositoryContext.HEADPath, $"ref: refs/heads/{branchName}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<(string fullPath, string branchName)> ReadHead()
    {
        try
        {
            // HEAD file contains something like: "ref: refs/heads/<branch>"
            var fileText = await File.ReadAllTextAsync(repositoryContext.HEADPath);
            var trimmed = fileText.Trim();
            var parts = trimmed.Split(':');
            var refPart = parts.Length > 1 ? parts[1].Trim() : trimmed; // handle unexpected format
            var branchName = refPart.Split('/').Last();
            return (refPart, branchName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task CreateBranch(string branchName)
    {
        if (!Directory.Exists(repositoryContext.HeadsPath)) 
            return;
        await fileSystem.WriteAtomic(Path.Combine(repositoryContext.HeadsPath, branchName), "");
    }

    public async Task DeleteBranch(string branchName)
    {
        var headsPath = Path.Combine(repositoryContext.HeadsPath, branchName);
        if (Directory.Exists(headsPath)) 
            return;
        var head = await ReadHead();
        if (head.branchName.Equals(branchName))
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Head branch can't be deleted!");
            Console.ResetColor();
            return;
        }
        Directory.Delete(headsPath, false);
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{branchName} deleted successfully!");
        Console.ResetColor();
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
        
        var head = await ReadHead();
        Console.WriteLine("Branches:");
        Console.WriteLine($"\tHEAD: {head.branchName}");
        foreach (var file in directory.GetFiles())
        {
            Console.WriteLine($"\t{file.Name}");
        }
    }
}