using System.CommandLine;
using GitClone.Application.Tag;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class TagCommand(
    TagUseCase tagUseCase,
    TagRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var nameArgument = new Argument<string?>("name")
        {
            Description = "Tag name to create at HEAD",
            Arity = ArgumentArity.ZeroOrOne
        };
        var deleteOption = new Option<string?>("--delete", "-d") { Description = "Delete the given tag" };

        var command = new Command("tag", "List, create, or delete tags");
        command.Arguments.Add(nameArgument);
        command.Options.Add(deleteOption);

        command.Validators.Add(result =>
        {
            if (result.GetValue(nameArgument) is not null && result.GetValue(deleteOption) is not null)
            {
                result.AddError("Specify only one of: <name> or --delete.");
            }
        });

        command.SetAction(async (parseResult, _) =>
        {
            var name = parseResult.GetValue(nameArgument);
            var deleteName = parseResult.GetValue(deleteOption);
            var request = new TagRequest(workingDirectoryProvider.GetCurrentDirectory(), name, deleteName);
            var result = await tagUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Succeeded ? 0 : 1;
        });

        return command;
    }
}
