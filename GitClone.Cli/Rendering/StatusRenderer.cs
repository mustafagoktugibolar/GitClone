using GitClone.Core.Abstractions;
using GitClone.Application.Status;

namespace GitClone.Cli.Rendering;

public sealed class StatusRenderer(IConsole console)
{
    public void Render(StatusResult result)
    {
        RenderSection("Untracked files", result.UntrackedFiles);
        RenderSection("Tracked clean files", result.TrackedCleanFiles);
        RenderSection("Modified files", result.ModifiedFiles);

        if (result.IsWorkingTreeClean)
        {
            console.WriteLine("Working tree clean.");
        }
    }

    private void RenderSection(string title, IReadOnlyList<string> files)
    {
        console.WriteLine($"{title}:");
        if (files.Count == 0)
        {
            console.WriteLine("  (none)");
            return;
        }

        foreach (var file in files)
        {
            console.WriteLine($"  {file}");
        }
    }
}
