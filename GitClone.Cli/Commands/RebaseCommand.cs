using System.CommandLine;
using GitClone.Application.Rebase;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class RebaseCommand(
    RebaseUseCase rebaseUseCase,
    RebaseRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var branchArgument = new Argument<string>("branch") { Description = "Branch to rebase the current branch onto" };

        var command = new Command("rebase", "Reapply the current branch's commits on top of another branch");
        command.Arguments.Add(branchArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var branch = parseResult.GetValue(branchArgument)!;
            var request = new RebaseRequest(workingDirectoryProvider.GetCurrentDirectory(), branch);
            var result = await rebaseUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Outcome == RebaseOutcome.Conflict || !result.Succeeded ? 1 : 0;
        });

        return command;
    }
}
