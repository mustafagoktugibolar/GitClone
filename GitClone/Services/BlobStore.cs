using GitClone.Interfaces;

namespace GitClone.Services;

public class BlobStore : IBlobStore
{
    private string _objectsPath;
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
        string repoPath = Path.Combine(Environment.CurrentDirectory, ".ilos");
        _objectsPath = Path.Combine(repoPath, "objects");
        if (!Directory.Exists(_objectsPath))
        {
            Directory.CreateDirectory(_objectsPath);
        }
    }
}
