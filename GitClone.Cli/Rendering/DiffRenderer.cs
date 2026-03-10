using GitClone.Application.Diff;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class DiffRenderer(IConsole console)
{
    public void Render(DiffResult result)
    {
        if (result.ShowUsage)
        {
            RenderUsage(result.Message);
            return;
        }

        if (!result.Succeeded)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message ?? "Unable to compute diff.");
            console.ResetColor();
            return;
        }

        if (result.Entries.Count == 0)
        {
            console.WriteLine("No differences.");
            return;
        }

        foreach (var entry in result.Entries)
        {
            console.WriteLine($"{entry.Status} {entry.Path}");
        }
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
        console.WriteLine("  ilos diff");
        console.WriteLine("  ilos diff --cached");
    }
}
