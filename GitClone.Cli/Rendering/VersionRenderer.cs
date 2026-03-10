using GitClone.Core.Abstractions;
using GitClone.Application.Version;

namespace GitClone.Cli.Rendering;

public sealed class VersionRenderer(IConsole console)
{
    public void Render(VersionResult result)
    {
        console.WriteLine($"ilos {result.VersionText}");
    }
}
