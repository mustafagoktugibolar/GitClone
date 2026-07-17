using System.CommandLine;
using GitClone.Application.Config;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public class ConfigCommand(
    ConfigUseCase configUseCase,
    ConfigRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var globalOption = new Option<bool>("--global", "-g")
        {
            Description = "Apply to the global configuration instead of the repository-local one",
            Recursive = true
        };

        var command = new Command("config", "Manage local/global configuration");
        command.Options.Add(globalOption);

        var listCommand = new Command("list", "List configured users");
        listCommand.SetAction((parseResult, cancellationToken) =>
            RunAsync(parseResult, globalOption, ["list"], cancellationToken));
        command.Subcommands.Add(listCommand);

        var addUsernameArgument = new Argument<string>("username") { Description = "Username" };
        var addEmailArgument = new Argument<string>("email") { Description = "Email address" };
        var addCommand = new Command("add", "Add a user");
        addCommand.Arguments.Add(addUsernameArgument);
        addCommand.Arguments.Add(addEmailArgument);
        addCommand.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult,
            globalOption,
            ["add", parseResult.GetValue(addUsernameArgument)!, parseResult.GetValue(addEmailArgument)!],
            cancellationToken));
        command.Subcommands.Add(addCommand);

        var removeEmailArgument = new Argument<string>("email") { Description = "Email address to remove" };
        var removeCommand = new Command("remove", "Remove a user");
        removeCommand.Aliases.Add("rm");
        removeCommand.Arguments.Add(removeEmailArgument);
        removeCommand.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult,
            globalOption,
            ["remove", parseResult.GetValue(removeEmailArgument)!],
            cancellationToken));
        command.Subcommands.Add(removeCommand);

        var editEmailArgument = new Argument<string>("email") { Description = "Email address to edit" };
        var usernameOption = new Option<string>("--username") { Description = "New username" };
        var emailOption = new Option<string>("--email") { Description = "New email address" };
        var passwordOption = new Option<bool>("--password") { Description = "Prompt for a new password" };
        var editCommand = new Command("edit", "Edit a user");
        editCommand.Arguments.Add(editEmailArgument);
        editCommand.Options.Add(usernameOption);
        editCommand.Options.Add(emailOption);
        editCommand.Options.Add(passwordOption);
        editCommand.SetAction((parseResult, cancellationToken) =>
        {
            var editArgs = new List<string> { "edit", parseResult.GetValue(editEmailArgument)! };

            var newUsername = parseResult.GetValue(usernameOption);
            if (newUsername is not null)
            {
                editArgs.Add("--username");
                editArgs.Add(newUsername);
            }

            var newEmail = parseResult.GetValue(emailOption);
            if (newEmail is not null)
            {
                editArgs.Add("--email");
                editArgs.Add(newEmail);
            }

            if (parseResult.GetValue(passwordOption))
            {
                editArgs.Add("--password");
            }

            return RunAsync(parseResult, globalOption, editArgs.ToArray(), cancellationToken);
        });
        command.Subcommands.Add(editCommand);

        return command;
    }

    private async Task<int> RunAsync(
        ParseResult parseResult,
        Option<bool> globalOption,
        string[] actionArgs,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var isGlobal = parseResult.GetValue(globalOption);

        // ConfigUseCase parses a raw args array shaped like the legacy CLI input; reconstruct
        // that shape here so the Application layer doesn't need to change.
        string[] legacyArgs = isGlobal
            ? ["config", "--global", .. actionArgs]
            : ["config", .. actionArgs];

        var request = new ConfigRequest(workingDirectoryProvider.GetCurrentDirectory(), legacyArgs);
        var result = await configUseCase.ExecuteAsync(request);
        renderer.Render(result);
        return 0;
    }
}
