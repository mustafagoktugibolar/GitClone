namespace GitClone.Interfaces;

public interface IIndexManager
{
    void StageFile(string fileName, string hash);
    void EnsureCreated();
}
