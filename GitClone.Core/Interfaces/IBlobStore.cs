namespace GitClone.Core.Interfaces;

public interface IBlobStore
{
    bool Exists(string hash);
    Task Save(string hash, string content);
    Task EnsureDirectory();
}
