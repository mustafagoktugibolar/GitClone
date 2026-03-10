using GitClone.Core.Abstractions;
using GitClone.Application.Help;

namespace GitClone.Cli.Rendering;

public sealed class HelpRenderer(IConsole console)
{
    public void Render(HelpResult result)
    {
        foreach (var line in result.Lines)
        {
            console.WriteLine(line);
        }
    }
}
