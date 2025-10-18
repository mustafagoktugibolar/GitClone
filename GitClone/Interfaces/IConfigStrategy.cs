namespace GitClone.Interfaces;

public interface IConfigStrategy
{
    bool CanExecute(string[] args);
    Task Execute(string[] args);
    void ShowUsage(string? error = null);
}