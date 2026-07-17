using System.CommandLine;
using GitClone.Application.Diff;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class DiffCommand(
    DiffUseCase diffUseCase,
    DiffRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var cachedOption = new Option<bool>("--cached", "--staged")
        {
            Description = "Show staged changes instead of the working tree"
        };

        var command = new Command("diff", "Show changes between commits, commit and working tree, etc");
        command.Options.Add(cachedOption);

        command.SetAction(async (parseResult, _) =>
        {
            var cached = parseResult.GetValue(cachedOption);
            var request = new DiffRequest(workingDirectoryProvider.GetCurrentDirectory(), cached);
            var result = await diffUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
