using GitClone.Interfaces;

namespace GitClone.Services;

public class FileSystem : IFileSystem
{
    public IEnumerable<string> GetTrackedFilesRecursively()
    {
        return Directory.GetFiles(
            Directory.GetCurrentDirectory(),
            "*",
            SearchOption.AllDirectories
        ).Where(path => !path.Contains(Path.Combine(".ilos")));
    }

    public string Read(string filePath)
    {
        return File.ReadAllText(filePath);
    }
}

