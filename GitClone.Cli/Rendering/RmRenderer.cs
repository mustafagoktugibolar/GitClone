using GitClone.Application.Rm;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class RmRenderer(IConsole console)
{
    public void Render(RmResult result)
    {
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
