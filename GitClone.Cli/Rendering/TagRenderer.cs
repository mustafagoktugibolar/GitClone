using GitClone.Application.Tag;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class TagRenderer(IConsole console)
{
    public void Render(TagResult result)
    {
        if (result.Operation == TagOperation.List)
        {
            console.WriteLine("Tags:");
            foreach (var tag in result.Tags ?? [])
            {
                console.WriteLine($"  {tag}");
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
