using GitClone.Core.Abstractions;

namespace GitClone.Infrastructure.Runtime;

public sealed class SystemConsole : IConsole
{
    public void Write(string value)
    {
        Console.Write(value);
    }

    public void WriteLine(string value)
    {
        Console.WriteLine(value);
    }

    public void WriteErrorLine(string value)
    {
        Console.Error.WriteLine(value);
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void SetForegroundColor(ConsoleColor color)
    {
        Console.ForegroundColor = color;
    }

    public void ResetColor()
    {
        Console.ResetColor();
    }
}
