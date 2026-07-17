using System.CommandLine;
using GitClone.Application.Reset;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class ResetCommand(
    ResetUseCase resetUseCase,
    ResetRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var softOption = new Option<bool>("--soft") { Description = "Move HEAD only" };
        var mixedOption = new Option<bool>("--mixed") { Description = "Move HEAD and reset the index (default)" };
        var hardOption = new Option<bool>("--hard") { Description = "Move HEAD, reset the index, and the working tree" };
        var revisionArgument = new Argument<string?>("revision")
        {
            Description = "Commit to reset to, e.g. HEAD~1",
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new Command("reset", "Reset current HEAD to the specified state");
        command.Options.Add(softOption);
        command.Options.Add(mixedOption);
        command.Options.Add(hardOption);
        command.Arguments.Add(revisionArgument);

        command.Validators.Add(result =>
        {
            var selectedCount = new[] { softOption, mixedOption, hardOption }
                .Count(option => result.GetValue(option));
            if (selectedCount > 1)
            {
                result.AddError("Only one of --soft, --mixed, --hard may be specified.");
            }
        });

        command.SetAction(async (parseResult, _) =>
        {
            var mode = parseResult.GetValue(softOption) ? ResetMode.Soft
                : parseResult.GetValue(hardOption) ? ResetMode.Hard
                : ResetMode.Mixed;
            var revision = parseResult.GetValue(revisionArgument);

            var request = new ResetRequest(workingDirectoryProvider.GetCurrentDirectory(), mode, revision);
            var result = await resetUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
