using System.CommandLine;
using GitClone.Application.Fetch;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class FetchCommand(
    FetchUseCase fetchUseCase,
    FetchRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var remoteArgument = new Argument<string>("remote") { Description = "Remote to fetch from" };
        var branchOption = new Option<string?>("--branch", "-b") { Description = "Only fetch this branch (default: all branches)" };

        var command = new Command("fetch", "Download commits and blobs from a remote");
        command.Arguments.Add(remoteArgument);
        command.Options.Add(branchOption);

        command.SetAction(async (parseResult, _) =>
        {
            var remote = parseResult.GetValue(remoteArgument)!;
            var branch = parseResult.GetValue(branchOption);
            var request = new FetchRequest(workingDirectoryProvider.GetCurrentDirectory(), remote, branch);
            var result = await fetchUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return result.Succeeded ? 0 : 1;
        });

        return command;
    }
}
