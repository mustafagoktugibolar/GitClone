using GitClone.Application.Stash;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class StashRenderer(IConsole console)
{
    public void Render(StashResult result)
    {
        if (result.Outcome == StashOutcome.Listed)
        {
            if (result.Entries is null || result.Entries.Count == 0)
            {
                console.WriteLine("No stash entries.");
                return;
            }

            foreach (var entry in result.Entries)
            {
                console.WriteLine($"stash@{{{entry.Index}}}: {entry.Message}");
            }

            return;
        }

        if (result.Outcome == StashOutcome.Conflict)
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

        console.WriteLine(result.Message);
    }
}
