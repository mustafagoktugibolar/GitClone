using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Services;

public class IndexManager(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IIndexManager
{
    private string IndexPath => repositoryContext.IndexPath;
    public Task StageFile(string fileName, string hash)
    {
        var normalizedPath = fileName.Replace('\\', '/');
        var lines = File.ReadAllLines(IndexPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(' '))
            .ToDictionary(parts => parts[0], parts => parts[1]);

        lines[normalizedPath] = hash;

        var updatedLines = lines.Select(l => $"{l.Key} {l.Value}");
        File.WriteAllLines(IndexPath, updatedLines);
        return Task.CompletedTask;
    }

    public async Task EnsureCreated()
    {
        if (!File.Exists(IndexPath))
        {
            await fileSystem.WriteAtomic(IndexPath, ""); 
        }
    }
}
