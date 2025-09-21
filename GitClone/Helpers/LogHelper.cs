using GitClone.Interfaces;

namespace GitClone.Helpers;

public class LogHelper : ILogHelper
{
    
    public void Error(string message, Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("-------------------ERROR-------------------");
        Console.ResetColor();
        Console.WriteLine($"[ERROR] {DateTime.UtcNow.ToString("o")} {message} {ex}");
        Console.WriteLine("-------------------------------------------");
    }

    public void Warning(string message)
    {
        throw new NotImplementedException();
    }

    public void Info(string message)
    {
        throw new NotImplementedException();
    }

    public void Debug(string message)
    {
        throw new NotImplementedException();
    }

    public void CreateLogFile()
    {
        throw new NotImplementedException();
    }
}