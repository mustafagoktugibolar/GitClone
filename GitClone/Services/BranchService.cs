using GitClone.Interfaces;

namespace GitClone.Services;

public class BranchService(IRepositoryContext repositoryContext) : IBranchService
{
    private readonly string _repositoryPath = repositoryContext.IlosPath;
    public void EnsureCreated()
    {
        if (!Directory.Exists(_repositoryPath)) 
            return;
        
        var headsPath = Path.Combine(_repositoryPath, "refs", "heads");
        if (Directory.Exists(headsPath)) 
            return;
        
        Directory.CreateDirectory(headsPath);
        CreateBranch("master");
        WriteHead("master");
    }

    public void WriteHead(string branchName)
    {
        try
        {
            File.WriteAllText(Path.Combine(_repositoryPath, "HEAD"), $"ref: refs/heads/{branchName}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public (string fullPath, string branchName) ReadHead()
    {
        try
        {
            var headPath = Path.Combine(_repositoryPath, "HEAD");
            var fileText = File.ReadAllText(headPath).Trim();
            var branchName = fileText.Split('/').Last();
            return (fileText, branchName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void CreateBranch(string branchName)
    {
        var headsPath = Path.Combine(_repositoryPath, "refs", "heads");
        if (!Directory.Exists(headsPath)) 
            return;
        File.WriteAllText(Path.Combine(headsPath, branchName), $"");
    }

    public void DeleteBranch(string branchName)
    {
        var headsPath = Path.Combine(_repositoryPath, "refs", "heads", branchName);
        if (Directory.Exists(headsPath)) 
            return;
        if (ReadHead().branchName.Equals(branchName))
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

    public void RenameBranch(string branchName, string newBranchName)
    {
        var headsPath = Path.Combine(_repositoryPath, "refs", "heads");
        var branchPath = Path.Combine(headsPath, branchName);
        if(!File.Exists(branchPath))
            return;
        var file = new FileInfo(branchPath);
        file.MoveTo(Path.Combine(headsPath, newBranchName));
        
        if (ReadHead().branchName.Equals(branchName))
        {
            WriteHead(newBranchName);
        }
    }

    public void ListBranches()
    {
        var directory = new DirectoryInfo(Path.Combine(_repositoryPath, "refs", "heads"));
        if (!directory.Exists) 
            return;
        
        Console.WriteLine("Branches:");
        Console.WriteLine($"\tHEAD: {ReadHead().branchName}");
        foreach (var file in directory.GetFiles())
        {
            Console.WriteLine($"\t{file.Name}");
        }
    }
}