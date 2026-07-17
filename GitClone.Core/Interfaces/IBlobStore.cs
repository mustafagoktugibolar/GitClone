namespace GitClone.Core.Interfaces;

public interface IBlobStore
{
    bool Exists(string hash);
    Task Save(string hash, byte[] content);
    Task EnsureDirectory();
}
