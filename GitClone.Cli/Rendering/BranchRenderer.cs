using GitClone.Core.Abstractions;
using GitClone.Application.Branch;

namespace GitClone.Cli.Rendering;

public sealed class BranchRenderer(IConsole console)
{
    public void Render(BranchResult result)
    {
        if (result.Operation == BranchOperation.List)
        {
            console.WriteLine("Branches:");
            var head = result.HeadBranch;
            foreach (var branch in result.Branches ?? [])
            {
                var prefix = branch == head ? "* " : "  ";
                console.WriteLine($"{prefix}{branch}");
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
