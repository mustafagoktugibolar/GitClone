using GitClone.Core.Abstractions;
using GitClone.Application.Config;

namespace GitClone.Cli.Rendering;

public sealed class ConfigRenderer(IConsole console)
{
    public void Render(ConfigResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            console.SetForegroundColor(ConsoleColor.Red);
            console.WriteLine(result.Message);
            console.ResetColor();
        }

        if (result.ShowUsage)
        {
            RenderUsage();
        }
    }

    private void RenderUsage()
    {
        console.WriteLine("Usage:");
        console.WriteLine("  ilos config [--global|-g] list|-l");
        console.WriteLine("  ilos config [--global|-g] add <username> <email>");
        console.WriteLine("  ilos config [--global|-g] remove|-rm <email>");
        console.WriteLine("  ilos config [--global|-g] edit <email> [--username <name>] [--email <email>] [--password]");
    }
}
