using System.CommandLine;
using GitClone.Core.Abstractions;
using GitClone.Application.Branch;
using GitClone.Cli.Rendering;

namespace GitClone.Cli.Commands;

public class BranchCommand(
    BranchUseCase branchUseCase,
    BranchRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var nameArgument = new Argument<string?>("name")
        {
            Description = "Branch name to create",
            Arity = ArgumentArity.ZeroOrOne
        };
        var deleteOption = new Option<string?>("--delete", "-d")
        {
            Description = "Delete the given branch"
        };
        var renameOption = new Option<string[]>("--rename", "-m")
        {
            Description = "Rename a branch: --rename <old> <new>",
            Arity = new ArgumentArity(2, 2),
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("branch", "List, create, or delete branches");
        command.Arguments.Add(nameArgument);
        command.Options.Add(deleteOption);
        command.Options.Add(renameOption);

        command.Validators.Add(result =>
        {
            var selectedCount = new[]
            {
                result.GetValue(nameArgument) is not null,
                result.GetValue(deleteOption) is not null,
                result.GetValue(renameOption) is { Length: 2 }
            }.Count(selected => selected);

            if (selectedCount > 1)
            {
                result.AddError("Specify only one of: <name>, --delete, --rename.");
            }
        });

        command.SetAction(async (parseResult, _) =>
        {
            var name = parseResult.GetValue(nameArgument);
            var deleteName = parseResult.GetValue(deleteOption);
            var rename = parseResult.GetValue(renameOption);

            // BranchUseCase parses a raw args array shaped like the legacy CLI input; reconstruct
            // that shape here so the Application layer doesn't need to change.
            string[] legacyArgs = rename is { Length: 2 }
                ? ["branch", "-m", rename[0], rename[1]]
                : deleteName is not null
                    ? ["branch", "-d", deleteName]
                    : name is not null
                        ? ["branch", name]
                        : ["branch"];

            var request = new BranchRequest(workingDirectoryProvider.GetCurrentDirectory(), legacyArgs);
            var result = await branchUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
