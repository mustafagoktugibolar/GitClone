namespace GitClone.Core.Abstractions;

public interface IRepositorySessionFactory
{
    IRepositorySession CreateForCurrentDirectory(bool forInit = false);
    IRepositorySession CreateForPath(string workingDirectory, bool forInit = false);
}
