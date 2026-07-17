using System.CommandLine;
using GitClone.Application.Merge;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class MergeCommand(
    MergeUseCase mergeUseCase,
    MergeRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var branchArgument = new Argument<string>("branch") { Description = "Branch to merge into the current branch" };

        var command = new Command("merge", "Merge a branch into the current branch");
        command.Arguments.Add(branchArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var branch = parseResult.GetValue(branchArgument)!;
            var request = new MergeRequest(workingDirectoryProvider.GetCurrentDirectory(), branch);
            var result = await mergeUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Outcome == MergeOutcome.Conflict || !result.Succeeded ? 1 : 0;
        });

        return command;
    }
}
