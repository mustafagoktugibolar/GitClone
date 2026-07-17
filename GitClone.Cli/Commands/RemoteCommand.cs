using System.CommandLine;
using GitClone.Application.Remote;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class RemoteCommand(
    RemoteUseCase remoteUseCase,
    RemoteRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var command = new Command("remote", "Manage remote repositories (local filesystem paths to another .ilos repository)");
        command.SetAction((_, ct) => RunAsync(RemoteAction.List, null, null, ct));

        var addNameArgument = new Argument<string>("name") { Description = "Name for the remote" };
        var addPathArgument = new Argument<string>("path") { Description = "Filesystem path to the remote's repository" };
        var addCommand = new Command("add", "Add a remote");
        addCommand.Arguments.Add(addNameArgument);
        addCommand.Arguments.Add(addPathArgument);
        addCommand.SetAction((parseResult, ct) => RunAsync(
            RemoteAction.Add, parseResult.GetValue(addNameArgument), parseResult.GetValue(addPathArgument), ct));
        command.Subcommands.Add(addCommand);

        var removeNameArgument = new Argument<string>("name") { Description = "Remote to remove" };
        var removeCommand = new Command("remove", "Remove a remote");
        removeCommand.Aliases.Add("rm");
        removeCommand.Arguments.Add(removeNameArgument);
        removeCommand.SetAction((parseResult, ct) => RunAsync(RemoteAction.Remove, parseResult.GetValue(removeNameArgument), null, ct));
        command.Subcommands.Add(removeCommand);

        var listCommand = new Command("list", "List remotes");
        listCommand.SetAction((_, ct) => RunAsync(RemoteAction.List, null, null, ct));
        command.Subcommands.Add(listCommand);

        return command;
    }

    private async Task<int> RunAsync(RemoteAction action, string? name, string? path, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var request = new RemoteRequest(workingDirectoryProvider.GetCurrentDirectory(), action, name, path);
        var result = await remoteUseCase.ExecuteAsync(request);
        renderer.Render(result);
        return result.Succeeded ? 0 : 1;
    }
}
