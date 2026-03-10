namespace GitClone.Core.Interfaces;

public interface IIgnoreService
{
    void EnsureCreated();
    bool IsIgnored(string filePath);
    
}