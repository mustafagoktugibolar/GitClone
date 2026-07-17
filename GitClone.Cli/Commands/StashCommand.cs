using System.CommandLine;
using GitClone.Application.Stash;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class StashCommand(
    StashUseCase stashUseCase,
    StashRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var messageOption = new Option<string?>("--message", "-m") { Description = "Description for the stash entry" };
        var command = new Command("stash", "Stash changes to tracked files and reset the working tree to HEAD");
        command.Options.Add(messageOption);
        command.SetAction((parseResult, ct) => RunAsync(StashAction.Push, parseResult.GetValue(messageOption), 0, ct));

        var pushMessageOption = new Option<string?>("--message", "-m") { Description = "Description for the stash entry" };
        var pushCommand = new Command("push", "Save changes and reset the working tree to HEAD");
        pushCommand.Options.Add(pushMessageOption);
        pushCommand.SetAction((parseResult, ct) => RunAsync(StashAction.Push, parseResult.GetValue(pushMessageOption), 0, ct));
        command.Subcommands.Add(pushCommand);

        var popIndexArgument = new Argument<int>("index")
        {
            Description = "Stash index, 0 = most recent",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => 0
        };
        var popCommand = new Command("pop", "Apply a stash entry and remove it");
        popCommand.Arguments.Add(popIndexArgument);
        popCommand.SetAction((parseResult, ct) => RunAsync(StashAction.Pop, null, parseResult.GetValue(popIndexArgument), ct));
        command.Subcommands.Add(popCommand);

        var applyIndexArgument = new Argument<int>("index")
        {
            Description = "Stash index, 0 = most recent",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => 0
        };
        var applyCommand = new Command("apply", "Apply a stash entry without removing it");
        applyCommand.Arguments.Add(applyIndexArgument);
        applyCommand.SetAction((parseResult, ct) => RunAsync(StashAction.Apply, null, parseResult.GetValue(applyIndexArgument), ct));
        command.Subcommands.Add(applyCommand);

        var listCommand = new Command("list", "List stash entries");
        listCommand.SetAction((_, ct) => RunAsync(StashAction.List, null, 0, ct));
        command.Subcommands.Add(listCommand);

        var dropIndexArgument = new Argument<int>("index")
        {
            Description = "Stash index, 0 = most recent",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => 0
        };
        var dropCommand = new Command("drop", "Discard a stash entry without applying it");
        dropCommand.Arguments.Add(dropIndexArgument);
        dropCommand.SetAction((parseResult, ct) => RunAsync(StashAction.Drop, null, parseResult.GetValue(dropIndexArgument), ct));
        command.Subcommands.Add(dropCommand);

        return command;
    }

    private async Task<int> RunAsync(StashAction action, string? message, int index, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var request = new StashRequest(workingDirectoryProvider.GetCurrentDirectory(), action, index, message);
        var result = await stashUseCase.ExecuteAsync(request);
        renderer.Render(result);
        return result.Outcome == StashOutcome.Conflict || !result.Succeeded ? 1 : 0;
    }
}
