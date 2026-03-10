using GitClone.Core.Abstractions;
using GitClone.Application.Add;

namespace GitClone.Cli.Rendering;

public sealed class AddRenderer(IConsole console)
{
    public void Render(AddResult result)
    {
        if (result.StagedFiles.Count == 0 && result.IgnoredFiles.Count == 0)
        {
            console.WriteLine("Nothing to stage.");
            return;
        }

        if (result.StagedFiles.Count > 0)
        {
            console.WriteLine("Staged files:");
            foreach (var file in result.StagedFiles)
            {
                console.WriteLine($"  {file.Path} {file.Hash}");
            }
        }

        if (result.IgnoredFiles.Count > 0)
        {
            console.WriteLine("Ignored files:");
            foreach (var file in result.IgnoredFiles.OrderBy(path => path, StringComparer.Ordinal))
            {
                console.WriteLine($"  {file}");
            }
        }
    }

    public void RenderUsage(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            console.WriteLine($"Error: {error}");
        }

        console.WriteLine("Usage: ilos add <file-path>|.");
    }
}
