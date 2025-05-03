namespace GitClone.Interfaces;

public interface IFileSystem
{
    IEnumerable<string> GetTrackedFilesRecursively();
    string Read(string filePath);
}
