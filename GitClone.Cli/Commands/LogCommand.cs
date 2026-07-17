using System.CommandLine;
using GitClone.Application.Log;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;

namespace GitClone.Cli.Commands;

public sealed class LogCommand(
    LogUseCase logUseCase,
    LogRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider)
{
    public Command Build()
    {
        var maxCountOption = new Option<int?>("--max-count", "-n")
        {
            Description = "Limit the number of commits to show"
        };
        maxCountOption.Validators.Add(result =>
        {
            var value = result.GetValue(maxCountOption);
            if (value is <= 0)
            {
                result.AddError("Max count must be a positive integer.");
            }
        });

        var command = new Command("log", "Show commit logs");
        command.Options.Add(maxCountOption);

        command.SetAction(async (parseResult, _) =>
        {
            var maxCount = parseResult.GetValue(maxCountOption);
            var request = new LogRequest(workingDirectoryProvider.GetCurrentDirectory(), maxCount);
            var result = await logUseCase.ExecuteAsync(request);
            renderer.Render(result);
            return 0;
        });

        return command;
    }
}
