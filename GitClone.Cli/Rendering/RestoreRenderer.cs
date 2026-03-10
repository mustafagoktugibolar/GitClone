using GitClone.Application.Restore;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class RestoreRenderer(IConsole console)
{
    public void Render(RestoreResult result)
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
        console.WriteLine("  ilos restore <path>");
        console.WriteLine("  ilos restore --staged <path>");
    }
}
