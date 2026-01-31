using GitClone.Interfaces;
using System.Text;

namespace GitClone.Services;

public class FileSystem(IRepositoryContext repositoryContext) : IFileSystem
{
    private static Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public IEnumerable<string> GetTrackedFilesRecursively()
    {
        return Directory.GetFiles(repositoryContext.IlosPath, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.Combine(".ilos")));
    }

    public async Task<string> Read(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
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
