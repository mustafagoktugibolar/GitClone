using GitClone.Application.Fetch;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class FetchRenderer(IConsole console)
{
    public void Render(FetchResult result)
    {
        if (!result.Succeeded)
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message);
            console.ResetColor();
            return;
        }

        console.WriteLine(result.Message);
        foreach (var updated in result.UpdatedRefs ?? [])
        {
            console.WriteLine($"  {updated}");
        }
    }
}
