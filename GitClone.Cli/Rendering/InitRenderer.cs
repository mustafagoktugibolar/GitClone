using GitClone.Core.Abstractions;
using GitClone.Application.Init;

namespace GitClone.Cli.Rendering;

public sealed class InitRenderer(IConsole console)
{
    public void Render(InitResult result)
    {
        console.SetForegroundColor(result.CreatedNewRepository ? ConsoleColor.Green : ConsoleColor.DarkYellow);
        console.WriteLine($"Repository ready at {result.RepositoryRoot}");
        console.ResetColor();
    }
}
