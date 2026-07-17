using System.CommandLine;
using GitClone.Application.CherryPick;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class CherryPickCommand(
    CherryPickUseCase cherryPickUseCase,
    CherryPickRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var commitArgument = new Argument<string>("commit") { Description = "Commit to apply onto the current branch" };

        var command = new Command("cherry-pick", "Apply the changes introduced by an existing commit");
        command.Arguments.Add(commitArgument);

        command.SetAction(async (parseResult, _) =>
        {
            var commitId = parseResult.GetValue(commitArgument)!;
            var request = new CherryPickRequest(workingDirectoryProvider.GetCurrentDirectory(), commitId);
            var result = await cherryPickUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Outcome == CherryPickOutcome.Conflict || !result.Succeeded ? 1 : 0;
        });

        return command;
    }
}
