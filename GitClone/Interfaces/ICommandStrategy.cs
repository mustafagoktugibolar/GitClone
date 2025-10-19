namespace GitClone.Interfaces;

public interface ICommandStrategy
{
    bool CanExecute(string[] args);
    Task Execute(string[] args);
    void ShowUsage(string? error = null);
}