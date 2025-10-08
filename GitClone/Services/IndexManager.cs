using GitClone.Interfaces;

namespace GitClone.Services;

public class IndexManager(IRepositoryContext repositoryContext) : IIndexManager
{
    private string IndexPath = string.Empty;
    public void StageFile(string fileName, string hash)
    {
        var lines = File.ReadAllLines(IndexPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(' '))
            .ToDictionary(parts => parts[0], parts => parts[1]);

        lines[fileName] = hash;

        var updatedLines = lines.Select(l => $"{l.Key} {l.Value}");
        File.WriteAllLines(IndexPath, updatedLines);

        Console.WriteLine($"Staged '{fileName}' as {hash}");
    }

    public void EnsureCreated()
    {
        string repoPath = repositoryContext.RootPath;
        IndexPath = repositoryContext.IndexPath;
        if (!File.Exists(IndexPath))
        {
            File.WriteAllText(IndexPath, ""); 
        }
    }
}
