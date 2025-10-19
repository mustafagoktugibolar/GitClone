namespace GitClone.Interfaces;

public interface IFileSystem
{
    IEnumerable<string> GetTrackedFilesRecursively();
    Task<string> Read(string filePath);
    Task WriteAtomic(string filePath, string content);
}
