namespace GitClone.Interfaces;

public interface IIndexManager
{
    Task StageFile(string fileName, string hash);
    Task EnsureCreated();
}
