namespace GitClone.Interfaces;

public interface IBlobStore
{
    bool Exists(string hash);
    void Save(string hash, string content);
}
