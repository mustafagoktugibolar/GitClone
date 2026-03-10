using GitClone.Core.Interfaces;
using System.Text;

namespace GitClone.Infrastructure.Services;

public class FileSystem(IRepositoryContext repositoryContext) : IFileSystem
{
    private static Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    
    public IEnumerable<string> GetTrackedFilesRecursively()
    {
        foreach (var path in Directory.EnumerateFiles(repositoryContext.RootPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(repositoryContext.RootPath, path).Replace('\\', '/');
            if (relativePath.StartsWith(".ilos/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return path;
        }
    }
    
    public IEnumerable<string> EnumerateFiles(string rootPath, string searchPattern, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(rootPath, searchPattern, searchOption);
    }
    
    public string[] GetFiles(string directoryPath)
    {
        return Directory.GetFiles(directoryPath);
    }
    
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
    
    public bool DirectoryExists(string directoryPath)
    {
        return Directory.Exists(directoryPath);
    }
    
    public void CreateDirectory(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
    }
    
    public void DeleteFile(string filePath)
    {
        File.Delete(filePath);
    }
    
    public void MoveFile(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath);
    }

    public async Task<string> Read(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }
    
    public async Task<string[]> ReadAllLines(string filePath)
    {
        return await File.ReadAllLinesAsync(filePath);
    }

    public async Task WriteAtomic(string filePath, string content)
    {
        var tempFilePath = Path.GetTempFileName();

        await using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        await using (var sw = new StreamWriter(fs, _utf8NoBom))
        {
            await sw.WriteAsync(content);
            await sw.FlushAsync();
            await fs.FlushAsync();
        }
        File.Copy(tempFilePath, filePath, overwrite: true);
        File.Delete(tempFilePath);
    }
}
