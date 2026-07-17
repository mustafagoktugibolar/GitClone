using GitClone.Application.Mv;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class MvRenderer(IConsole console)
{
    public void Render(MvResult result)
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
