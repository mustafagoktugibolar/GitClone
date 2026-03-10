using GitClone.Application.Commit;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class CommitCommand(
    CommitUseCase commitUseCase,
    CommitRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("commit", StringComparison.OrdinalIgnoreCase);
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

        var parsed = ParseMessage(args);
        if (!parsed.Succeeded)
        {
            renderer.RenderUsage(parsed.ErrorMessage);
            return;
        }

        var request = new CommitRequest(workingDirectoryProvider.GetCurrentDirectory(), parsed.Message!);
        var result = await commitUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult ParseMessage(IReadOnlyList<string> args)
    {
        string? message = null;

        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "-m":
                case "--message":
                    if (i + 1 >= args.Count)
                    {
                        return ParseResult.Failed("Missing value for commit message.");
                    }

                    message = args[++i];
                    break;
                default:
                    return ParseResult.Failed($"Unknown option: {args[i]}");
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return ParseResult.Failed("Commit message is required.");
        }

        return ParseResult.Success(message);
    }

    private sealed record ParseResult(bool Succeeded, string? Message, string? ErrorMessage)
    {
        public static ParseResult Success(string message) => new(true, message, null);

        public static ParseResult Failed(string errorMessage) => new(false, null, errorMessage);
    }
}
