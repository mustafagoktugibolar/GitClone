using GitClone.Interfaces;

namespace GitClone.Services;

public class BlobStore(IRepositoryContext repositoryContext, IFileSystem fileSystem) : IBlobStore
{
    private string ObjectsPath => repositoryContext.ObjectsPath;
    public bool Exists(string hash)
    {
        return File.Exists(Path.Combine(ObjectsPath, hash));
    }

    public async Task Save(string hash, string content)
    {
        await fileSystem.WriteAtomic(Path.Combine(ObjectsPath, hash), content);
    }

    public Task EnsureDirectory()
    {
        if (!Directory.Exists(ObjectsPath))
        {
            Directory.CreateDirectory(ObjectsPath);
        }
        return Task.CompletedTask;
    }
}
