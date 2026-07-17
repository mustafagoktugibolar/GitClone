using System.CommandLine;
using GitClone.Application.Mv;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class MvCommand(
    MvUseCase mvUseCase,
    MvRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var sourceArgument = new Argument<string>("source") { Description = "File to rename or move" };
        var destinationArgument = new Argument<string>("destination") { Description = "New path for the file" };

        var command = new Command("mv", "Move or rename a file, updating the index");
        command.Arguments.Add(sourceArgument);
        command.Arguments.Add(destinationArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var source = parseResult.GetValue(sourceArgument)!;
            var destination = parseResult.GetValue(destinationArgument)!;
            var request = new MvRequest(workingDirectoryProvider.GetCurrentDirectory(), source, destination);
            var result = await mvUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Succeeded ? 0 : 1;
        });

        return command;
    }
}
