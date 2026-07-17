using System.CommandLine;
using GitClone.Application.Pull;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class PullCommand(
    PullUseCase pullUseCase,
    PullRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var remoteArgument = new Argument<string>("remote") { Description = "Remote to pull from" };
        var branchArgument = new Argument<string>("branch") { Description = "Branch to fetch and merge" };

        var command = new Command("pull", "Fetch from a remote and merge into the current branch");
        command.Arguments.Add(remoteArgument);
        command.Arguments.Add(branchArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var remote = parseResult.GetValue(remoteArgument)!;
            var branch = parseResult.GetValue(branchArgument)!;
            var request = new PullRequest(workingDirectoryProvider.GetCurrentDirectory(), remote, branch);
            var result = await pullUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Outcome == PullOutcome.Conflict || !result.Succeeded ? 1 : 0;
        });

        return command;
    }
}
