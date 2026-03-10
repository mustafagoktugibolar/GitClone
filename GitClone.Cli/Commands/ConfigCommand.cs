using GitClone.Application.Config;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public class ConfigCommand(
    ConfigUseCase configUseCase,
    ConfigRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("config", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        var request = new ConfigRequest(workingDirectoryProvider.GetCurrentDirectory(), args);
        var result = await configUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }
}
