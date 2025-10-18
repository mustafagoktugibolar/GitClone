using GitClone.Helpers;
using GitClone.Interfaces;
using GitClone.Models;

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
            var fileText = File.ReadAllTextAsync(repositoryContext.HeadsPath).Result.Trim();
            var branchName = fileText.Split('/').Last();
            return (fileText, branchName);
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
        await fileSystem.WriteAtomic(Path.Combine(repositoryContext.HeadsPath, branchName), $"");
    }

    public async Task DeleteBranch(string branchName)
    {
        var headsPath = Path.Combine(repositoryContext.HeadsPath, branchName);
        if (Directory.Exists(headsPath)) 
            return;
        if (ReadHead().Result.branchName.Equals(branchName))
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
        
        if (ReadHead().Result.branchName.Equals(branchName))
        {
            await WriteHead(newBranchName);
        }
    }

    public async Task ListBranches()
    {
        var directory = new DirectoryInfo(repositoryContext.HeadsPath);
        if (!directory.Exists) 
            return;
        
        Console.WriteLine("Branches:");
        Console.WriteLine($"\tHEAD: {ReadHead().Result.branchName}");
        foreach (var file in directory.GetFiles())
        {
            Console.WriteLine($"\t{file.Name}");
        }
    }
}