using System.CommandLine;
using GitClone.Application.Switch;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class SwitchCommand(
    SwitchUseCase switchUseCase,
    SwitchRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var branchArgument = new Argument<string>("branch") { Description = "Branch to switch to" };
        var createOption = new Option<bool>("--create", "-b") { Description = "Create the branch if it doesn't exist" };

        var command = new Command("switch", "Switch branches");
        command.Aliases.Add("checkout");
        command.Arguments.Add(branchArgument);
        command.Options.Add(createOption);

        command.SetAction(async (parseResult, _) =>
        {
            var branch = parseResult.GetValue(branchArgument)!;
            var create = parseResult.GetValue(createOption);

            var request = new SwitchRequest(workingDirectoryProvider.GetCurrentDirectory(), branch, create);
            var result = await switchUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
