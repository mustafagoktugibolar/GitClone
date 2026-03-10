using GitClone.Core.Abstractions;
using GitClone.Application.Clone;

namespace GitClone.Cli.Rendering;

public sealed class CloneRenderer(IConsole console)
{
    public void Render(CloneResult result)
    {
        console.SetForegroundColor(ConsoleColor.Green);
        console.WriteLine("Repository successfully cloned.");
        console.ResetColor();
        console.WriteLine($"  Name: {result.RepositoryName}");
        console.WriteLine($"  Branch: {result.BranchName}");
        console.WriteLine($"  Path: {result.TargetDirectory}");
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
        console.WriteLine("  ilos clone <url>");
        console.WriteLine("  ilos clone <url> --location <project destination>");
        console.WriteLine("  ilos clone <url> --branch <branch name>");
        console.WriteLine("  ilos clone <url> --name <project name>");
        console.WriteLine("  ilos clone <url> --name <project name> --branch <branch name> --location <project destination>");
    }
}
