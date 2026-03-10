namespace GitClone.Core.Interfaces;

public interface IFileSystem
{
    IEnumerable<string> GetTrackedFilesRecursively();
    IEnumerable<string> EnumerateFiles(string rootPath, string searchPattern, SearchOption searchOption);
    string[] GetFiles(string directoryPath);
    bool FileExists(string filePath);
    bool DirectoryExists(string directoryPath);
    void CreateDirectory(string directoryPath);
    void DeleteFile(string filePath);
    void MoveFile(string sourcePath, string destinationPath);
    Task<string> Read(string filePath);
    Task<string[]> ReadAllLines(string filePath);
    Task WriteAtomic(string filePath, string content);
}
