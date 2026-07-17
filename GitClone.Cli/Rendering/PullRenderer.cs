using GitClone.Application.Pull;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class PullRenderer(IConsole console)
{
    public void Render(PullResult result)
    {
        if (result.Outcome == PullOutcome.Conflict)
        {
            console.SetForegroundColor(ConsoleColor.Yellow);
            console.WriteLine(result.Message);
            console.ResetColor();

            foreach (var conflict in result.Conflicts ?? [])
            {
                console.WriteLine(conflict.MarkersWritten
                    ? $"  conflict: {conflict.Path}"
                    : $"  conflict: {conflict.Path} (binary file - resolve manually, no markers written)");
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
