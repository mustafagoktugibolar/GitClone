using GitClone.Application.Rebase;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class RebaseRenderer(IConsole console)
{
    public void Render(RebaseResult result)
    {
        if (result.Outcome == RebaseOutcome.Conflict)
        {
            console.SetForegroundColor(ConsoleColor.Yellow);
            console.WriteLine(result.Message);
            console.ResetColor();

            foreach (var conflict in result.Conflicts ?? [])
            {
                console.WriteLine($"  conflict: {conflict.Path}");
            }

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
        console.WriteLine(result.Message);
        console.ResetColor();
    }
}
