using GitClone.Interfaces;

namespace GitClone.Services;

public class IndexManager(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IIndexManager
{
    private string IndexPath => repositoryContext.IndexPath;
    public async Task StageFile(string fileName, string hash)
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

    public async Task EnsureCreated()
    {
        if (!File.Exists(IndexPath))
        {
            await fileSystem.WriteAtomic(IndexPath, ""); 
        }
    }
}
