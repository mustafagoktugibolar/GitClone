using GitClone.Interfaces;

namespace GitClone.Services;

public class BlobStore : IBlobStore
{
    private readonly string _objectsPath;

    public BlobStore()
    {
        string repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _objectsPath = Path.Combine(repoPath, "objects");
    }

    public bool Exists(string hash)
    {
        return File.Exists(Path.Combine(_objectsPath, hash));
    }

    public void Save(string hash, string content)
    {
        File.WriteAllText(Path.Combine(_objectsPath, hash), content);
    }

    public void EnsureDirectory()
    {
        if (!Directory.Exists(_objectsPath))
        {
            Directory.CreateDirectory(_objectsPath);
        }
    }
}
