using GitClone.Application.Reset;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class ResetRenderer(IConsole console)
{
    public void Render(ResetResult result)
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
        console.WriteLine("  ilos reset [--soft|--mixed|--hard] [<revision>]");
        console.WriteLine("Examples:");
        console.WriteLine("  ilos reset --hard HEAD~1");
        console.WriteLine("  ilos reset --mixed <commit-id>");
    }
}
