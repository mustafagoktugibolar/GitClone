using GitClone.Application.Log;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class LogRenderer(IConsole console)
{
    public void Render(LogResult result)
    {
        if (result.ShowUsage)
        {
            RenderUsage(result.Message);
            return;
        }

        if (!result.Succeeded)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message ?? "Unable to read commit log.");
            console.ResetColor();
            return;
        }

        if (result.Entries.Count == 0)
        {
            console.WriteLine($"No commits yet on '{result.BranchName}'.");
            return;
        }

        foreach (var entry in result.Entries)
        {
            console.WriteLine(entry.IsMergeCommit ? $"commit {entry.CommitId} (merge)" : $"commit {entry.CommitId}");
            console.WriteLine($"Author: {entry.AuthorName} <{entry.AuthorEmail}>");
            console.WriteLine($"Date:   {entry.CommittedAtUtc:O}");
            console.WriteLine(string.Empty);
            console.WriteLine($"    {entry.Message}");
            console.WriteLine(string.Empty);
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
        console.WriteLine("  ilos log");
        console.WriteLine("  ilos log -n <count>");
    }
}
