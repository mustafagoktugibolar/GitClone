using GitClone.Interfaces;

namespace GitClone.Services;

public class BlobStore(IRepositoryContext repositoryContext) : IBlobStore
{
    private string ObjectsPath = string.Empty;
    public bool Exists(string hash)
    {
        return File.Exists(Path.Combine(ObjectsPath, hash));
    }

    public void Save(string hash, string content)
    {
        File.WriteAllText(Path.Combine(ObjectsPath, hash), content);
    }

    public void EnsureDirectory()
    {
        string repoPath = repositoryContext.RootPath;
        ObjectsPath = repositoryContext.ObjectsPath;
        if (!Directory.Exists(ObjectsPath))
        {
            Directory.CreateDirectory(ObjectsPath);
        }
    }
}
