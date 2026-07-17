using System.CommandLine;
using GitClone.Application.Push;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class PushCommand(
    PushUseCase pushUseCase,
    PushRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var remoteArgument = new Argument<string>("remote") { Description = "Remote to push to" };
        var branchArgument = new Argument<string>("branch") { Description = "Branch to push" };

        var command = new Command("push", "Update a remote branch with local commits");
        command.Arguments.Add(remoteArgument);
        command.Arguments.Add(branchArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var remote = parseResult.GetValue(remoteArgument)!;
            var branch = parseResult.GetValue(branchArgument)!;
            var request = new PushRequest(workingDirectoryProvider.GetCurrentDirectory(), remote, branch);
            var result = await pushUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Succeeded ? 0 : 1;
        });

        return command;
    }
}
