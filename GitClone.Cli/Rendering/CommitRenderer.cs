using GitClone.Application.Commit;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class CommitRenderer(IConsole console)
{
    public void Render(CommitResult result)
    {
        if (result.ShowUsage)
        {
            RenderUsage(result.Message);
            return;
        }

        if (result.NothingToCommit)
        {
            console.WriteLine(result.Message);
            return;
        }

        if (!result.Succeeded)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message);
            console.ResetColor();
            return;
        }

        console.SetForegroundColor(ConsoleColor.Green);
        console.WriteLine($"[{result.BranchName} {result.CommitId}] {result.Message}");
        console.ResetColor();
        console.WriteLine($"{result.ChangedFiles} file(s) changed.");
    }

    public void RenderUsage(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine($"Error: {error}");
            console.ResetColor();
        }

        console.WriteLine("Usage: ilos commit -m <message>");
    }
}
