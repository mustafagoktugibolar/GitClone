using GitClone.Application.Switch;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class SwitchRenderer(IConsole console)
{
    public void Render(SwitchResult result)
    {
        if (result.ShowUsage)
        {
            RenderUsage(result.Message);
            return;
        }

        if (!result.Succeeded)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message);
            console.ResetColor();
            return;
        }

        console.WriteLine(result.Message);
    }

    public void RenderUsage(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine($"Error: {error}");
            console.ResetColor();
        }

        console.WriteLine("Usage:");
        console.WriteLine("  ilos switch <branch>");
        console.WriteLine("  ilos switch -b <new-branch>");
        console.WriteLine("  ilos checkout <branch>");
        console.WriteLine("  ilos checkout -b <new-branch>");
    }
}
