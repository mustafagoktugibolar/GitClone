namespace GitClone.Core.Interfaces;

public interface IIndexManager
{
    Task StageFile(string fileName, string hash);
    Task EnsureCreated();
}
