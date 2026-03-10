using GitClone.Application.Diff;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class DiffCommand(
    DiffUseCase diffUseCase,
    DiffRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("diff", StringComparison.OrdinalIgnoreCase);
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

        var request = new DiffRequest(workingDirectoryProvider.GetCurrentDirectory(), parsed.Cached);
        var result = await diffUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult Parse(IReadOnlyList<string> args)
    {
        var cached = false;
        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--cached":
                case "--staged":
                    cached = true;
                    break;
                default:
                    return ParseResult.Failed($"Unknown option: {args[i]}");
            }
        }

        return ParseResult.Success(cached);
    }

    private sealed record ParseResult(bool Succeeded, bool Cached, string? ErrorMessage)
    {
        public static ParseResult Success(bool cached) => new(true, cached, null);
        public static ParseResult Failed(string error) => new(false, false, error);
    }
}
