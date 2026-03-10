using GitClone.Core.Abstractions;
using GitClone.Application.Status;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public class StatusCommand(
    StatusUseCase statusUseCase,
    StatusRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("status", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        var request = new StatusRequest(workingDirectoryProvider.GetCurrentDirectory());
        var result = await statusUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }
}
