namespace GitClone.Interfaces;

public interface ICommandStrategy
{
    bool CanExecute(string[] args);
    void Execute(string[] args);
    void ShowUsage(string? error = null);
}