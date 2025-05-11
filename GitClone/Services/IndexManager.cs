using GitClone.Interfaces;

namespace GitClone.Services;

public class IndexManager : IIndexManager
{
    private readonly string _indexPath;

    public IndexManager()
    {
        string repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _indexPath = Path.Combine(repoPath, "index");
    }

    public void StageFile(string fileName, string hash)
    {
        var lines = File.ReadAllLines(_indexPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(' '))
            .ToDictionary(parts => parts[0], parts => parts[1]);

        lines[fileName] = hash;

        var updatedLines = lines.Select(l => $"{l.Key} {l.Value}");
        File.WriteAllLines(_indexPath, updatedLines);

        Console.WriteLine($"Staged '{fileName}' as {hash}");
    }

    public void EnsureCreated()
    {
        if (!File.Exists(_indexPath))
        {
            File.WriteAllText(_indexPath, ""); 
        }
    }
}
