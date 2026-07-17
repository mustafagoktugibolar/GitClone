using GitClone.Core.Interfaces;
using System.Text;

namespace GitClone.Infrastructure.Services;

public class FileSystem(IRepositoryContext repositoryContext) : IFileSystem
{
    private static readonly Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    
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
        await WriteAtomicCore(filePath, async tempFilePath =>
        {
            await using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var sw = new StreamWriter(fs, _utf8NoBom);
            await sw.WriteAsync(content);
            await sw.FlushAsync();
            await fs.FlushAsync();
        });
    }

    public async Task<byte[]> ReadBytes(string filePath)
    {
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task WriteAtomicBytes(string filePath, byte[] content)
    {
        await WriteAtomicCore(filePath, async tempFilePath =>
        {
            await using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fs.WriteAsync(content);
            await fs.FlushAsync();
        });
    }

    /// <summary>
    /// Writes to a temp file in the same directory as <paramref name="filePath"/> (so the final
    /// rename stays on one volume) and then swaps it into place with <see cref="File.Move(string, string, bool)"/>,
    /// which performs an atomic replace on both POSIX and NTFS. This avoids the previous approach of
    /// writing to the system temp directory (a different volume on many setups, defeating atomicity)
    /// and then File.Copy-ing over the target, which is not an atomic operation and can leave a
    /// corrupted target file if the process is interrupted mid-copy.
    /// </summary>
    private static async Task WriteAtomicCore(string filePath, Func<string, Task> writeToTempFile)
    {
        var directory = Path.GetDirectoryName(filePath);
        var tempFilePath = string.IsNullOrEmpty(directory)
            ? $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp"
            : Path.Combine(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await writeToTempFile(tempFilePath);
            File.Move(tempFilePath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
