using System.CommandLine;
using GitClone.Application.Restore;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class RestoreCommand(
    RestoreUseCase restoreUseCase,
    RestoreRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var pathArgument = new Argument<string>("path") { Description = "Path to restore" };
        var stagedOption = new Option<bool>("--staged") { Description = "Restore the index only, not the working tree" };

        var command = new Command("restore", "Restore working tree files");
        command.Arguments.Add(pathArgument);
        command.Options.Add(stagedOption);

        command.SetAction(async (parseResult, _) =>
        {
            var path = parseResult.GetValue(pathArgument)!;
            var staged = parseResult.GetValue(stagedOption);

            var request = new RestoreRequest(workingDirectoryProvider.GetCurrentDirectory(), path, staged);
            var result = await restoreUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
