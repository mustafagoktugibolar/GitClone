using System.CommandLine;
using GitClone.Core.Abstractions;
using GitClone.Application.Clone;
using GitClone.Cli.Rendering;

namespace GitClone.Cli.Commands;

public class CloneCommand(
    CloneUseCase cloneUseCase,
    CloneRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var urlArgument = new Argument<string>("url") { Description = "Repository URL to clone" };
        var nameOption = new Option<string>("--name") { Description = "Target project name" };
        var branchOption = new Option<string>("--branch") { Description = "Branch to clone" };
        var locationOption = new Option<string>("--location") { Description = "Destination directory" };

        var command = new Command("clone", "Clone a repository into a new directory");
        command.Arguments.Add(urlArgument);
        command.Options.Add(nameOption);
        command.Options.Add(branchOption);
        command.Options.Add(locationOption);

        command.SetAction(async (parseResult, _) =>
        {
            var request = new CloneRequest(
                workingDirectoryProvider.GetCurrentDirectory(),
                parseResult.GetValue(urlArgument)!,
                parseResult.GetValue(nameOption),
                parseResult.GetValue(branchOption),
                parseResult.GetValue(locationOption));

            var result = await cloneUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
