using GitClone.Core.Abstractions;
using GitClone.Application.Branch;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public class BranchCommand(
    BranchUseCase branchUseCase,
    BranchRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("branch", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        var request = new BranchRequest(workingDirectoryProvider.GetCurrentDirectory(), args);
        var result = await branchUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }
}
    
