using System.CommandLine;
using GitClone.Application.Rm;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class RmCommand(
    RmUseCase rmUseCase,
    RmRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var pathArgument = new Argument<string>("path") { Description = "Tracked file to remove" };
        var cachedOption = new Option<bool>("--cached") { Description = "Remove from the index only; keep the working tree file" };

        var command = new Command("rm", "Remove a file from the working tree and the index");
        command.Arguments.Add(pathArgument);
        command.Options.Add(cachedOption);

        command.SetAction(async (parseResult, _) =>
        {
            var path = parseResult.GetValue(pathArgument)!;
            var cached = parseResult.GetValue(cachedOption);
            var request = new RmRequest(workingDirectoryProvider.GetCurrentDirectory(), path, cached);
            var result = await rmUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Succeeded ? 0 : 1;
        });

        return command;
    }
}
