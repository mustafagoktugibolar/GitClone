using GitClone.Interfaces;

namespace GitClone.Services;

public class FileSystem(IRepositoryContext repositoryContext) : IFileSystem
{
    public IEnumerable<string> GetTrackedFilesRecursively()
    {
        return Directory.GetFiles(repositoryContext.IlosPath, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.Combine(".ilos")));
    }

    public string Read(string filePath)
    {
        return File.ReadAllText(filePath);
    }
}

