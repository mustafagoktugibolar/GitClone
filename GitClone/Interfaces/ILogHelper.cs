namespace GitClone.Interfaces;

public interface ILogHelper
{
    void Error(string message, Exception ex);
    void Warning(string message);
    void Info(string message);
    void Debug(string message);
    void CreateLogFile();
}