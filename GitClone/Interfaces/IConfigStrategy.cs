namespace GitClone.Interfaces;

public interface IConfigStrategy
{
    bool CanExecute(string[] args);
    void Execute(string[] args);
}