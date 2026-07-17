using GitClone.Application.Push;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class PushRenderer(IConsole console)
{
    public void Render(PushResult result)
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
