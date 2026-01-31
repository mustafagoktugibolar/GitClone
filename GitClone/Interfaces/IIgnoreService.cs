namespace GitClone.Interfaces;

public interface IIgnoreService
{
    void EnsureCreated();
    bool IsIgnored(string filePath);
    
}