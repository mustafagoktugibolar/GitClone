namespace GitClone.Core.Abstractions;

public interface IConsole
{
    void Write(string value);
    void WriteLine(string value);
    void WriteErrorLine(string value);
    string? ReadLine();
    void SetForegroundColor(ConsoleColor color);
    void ResetColor();
}
