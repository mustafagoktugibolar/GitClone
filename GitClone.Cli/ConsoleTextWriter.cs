using System.Text;
using GitClone.Core.Abstractions;

namespace GitClone.Cli;

/// <summary>
/// Adapts a line-oriented write delegate (typically <see cref="IConsole"/>'s WriteLine/WriteErrorLine)
/// to a <see cref="TextWriter"/>, so that System.CommandLine's built-in help, version, and parse-error
/// output goes through the same console abstraction as the rest of the app - keeping it testable via
/// IConsole instead of writing straight to <see cref="Console"/>.
/// </summary>
internal sealed class ConsoleTextWriter(Action<string> writeLine) : TextWriter
{
    private readonly StringBuilder _buffer = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            FlushLine();
        }
        else if (value != '\r')
        {
            _buffer.Append(value);
        }
    }

    public override void Flush()
    {
        if (_buffer.Length > 0)
        {
            FlushLine();
        }
    }

    private void FlushLine()
    {
        writeLine(_buffer.ToString());
        _buffer.Clear();
    }
}
