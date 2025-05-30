namespace GitClone.Interfaces;

public interface ILogHelper
{
    void EnsureCreated();
    void Error(string message);
    void Warning(string message);
    void Info(string message);
    void Debug(string message);
    void CreateLogFile();
}