using System.CommandLine;
using GitClone.Core.Abstractions;
using GitClone.Application.Status;
using GitClone.Cli.Rendering;

namespace GitClone.Cli.Commands;

public class StatusCommand(
    StatusUseCase statusUseCase,
    StatusRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var command = new Command("status", "Show the working tree status");

        command.SetAction(async (_, _) =>
        {
            var request = new StatusRequest(workingDirectoryProvider.GetCurrentDirectory());
            var result = await statusUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
