using GitClone.Application.Log;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class LogCommand(
    LogUseCase logUseCase,
    LogRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("log", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        if (args.Length == 2 &&
            (args[1].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
             args[1].Equals("help", StringComparison.OrdinalIgnoreCase)))
        {
            renderer.RenderUsage();
            return;
        }

        var parsed = Parse(args);
        if (!parsed.Succeeded)
        {
            renderer.RenderUsage(parsed.ErrorMessage);
            return;
        }

        var request = new LogRequest(workingDirectoryProvider.GetCurrentDirectory(), parsed.MaxCount);
        var result = await logUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult Parse(IReadOnlyList<string> args)
    {
        int? maxCount = null;

        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "-n":
                case "--max-count":
                    if (i + 1 >= args.Count)
                    {
                        return ParseResult.Failed("Missing value for max count.");
                    }

                    if (!int.TryParse(args[++i], out var parsed) || parsed <= 0)
                    {
                        return ParseResult.Failed("Max count must be a positive integer.");
                    }

                    maxCount = parsed;
                    break;
                default:
                    return ParseResult.Failed($"Unknown option: {args[i]}");
            }
        }

        return ParseResult.Success(maxCount);
    }

    private sealed record ParseResult(bool Succeeded, int? MaxCount, string? ErrorMessage)
    {
        public static ParseResult Success(int? maxCount) => new(true, maxCount, null);

        public static ParseResult Failed(string error) => new(false, null, error);
    }
}
