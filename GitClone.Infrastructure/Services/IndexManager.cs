using GitClone.Core.Interfaces;

namespace GitClone.Infrastructure.Services;

public class IndexManager(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IIndexManager
{
    private string IndexPath => repositoryContext.IndexPath;
    public async Task StageFile(string fileName, string hash)
    {
        var normalizedPath = fileName.Replace('\\', '/');
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (fileSystem.FileExists(IndexPath))
        {
            var lines = await fileSystem.ReadAllLines(IndexPath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Split on the last space - paths may themselves contain spaces, but the hash
                // (the last token) never does, so the rightmost space is the only delimiter
                // position that's unambiguous.
                var separator = line.LastIndexOf(' ');
                if (separator <= 0 || separator >= line.Length - 1)
                {
                    continue;
                }

                entries[line[..separator]] = line[(separator + 1)..];
            }
        }

        entries[normalizedPath] = hash;

        var updatedLines = entries
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key} {entry.Value}");
        await fileSystem.WriteAtomic(IndexPath, string.Join(Environment.NewLine, updatedLines));
    }

    public async Task EnsureCreated()
    {
        if (!File.Exists(IndexPath))
        {
            await fileSystem.WriteAtomic(IndexPath, ""); 
        }
    }
}
