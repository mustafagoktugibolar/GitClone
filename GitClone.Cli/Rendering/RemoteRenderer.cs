using GitClone.Application.Remote;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Rendering;

public sealed class RemoteRenderer(IConsole console)
{
    public void Render(RemoteResult result)
    {
        if (result.Action == RemoteAction.List)
        {
            if (result.Remotes is null || result.Remotes.Count == 0)
            {
                console.WriteLine("No remotes.");
                return;
            }

            foreach (var remote in result.Remotes)
            {
                console.WriteLine($"{remote.Name}\t{remote.Path}");
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
