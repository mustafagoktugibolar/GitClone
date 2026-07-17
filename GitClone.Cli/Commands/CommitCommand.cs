using System.CommandLine;
using GitClone.Application.Commit;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class CommitCommand(
    CommitUseCase commitUseCase,
    CommitRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var messageOption = new Option<string>("--message", "-m")
        {
            Description = "Commit message",
            Required = true
        };
        messageOption.Validators.Add(result =>
        {
            var value = result.GetValue(messageOption);
            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError("Commit message must not be empty.");
            }
        });

        var command = new Command("commit", "Record staged changes");
        command.Options.Add(messageOption);

        command.SetAction(async (parseResult, _) =>
        {
            var message = parseResult.GetValue(messageOption)!;
            var request = new CommitRequest(workingDirectoryProvider.GetCurrentDirectory(), message);
            var result = await commitUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
