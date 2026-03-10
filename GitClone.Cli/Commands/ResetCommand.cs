using GitClone.Application.Reset;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class ResetCommand(
    ResetUseCase resetUseCase,
    ResetRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("reset", StringComparison.OrdinalIgnoreCase);
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

        var request = new ResetRequest(
            workingDirectoryProvider.GetCurrentDirectory(),
            parsed.Mode,
            parsed.TargetSpec);

        var result = await resetUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult Parse(IReadOnlyList<string> args)
    {
        var mode = ResetMode.Mixed;
        string? targetSpec = null;

        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--soft":
                    mode = ResetMode.Soft;
                    break;
                case "--mixed":
                    mode = ResetMode.Mixed;
                    break;
                case "--hard":
                    mode = ResetMode.Hard;
                    break;
                default:
                    if (targetSpec != null)
                    {
                        return ParseResult.Failed("Only a single revision is supported.");
                    }

                    targetSpec = args[i];
                    break;
            }
        }

        return ParseResult.Success(mode, targetSpec);
    }

    private sealed record ParseResult(bool Succeeded, ResetMode Mode, string? TargetSpec, string? ErrorMessage)
    {
        public static ParseResult Success(ResetMode mode, string? targetSpec)
        {
            return new ParseResult(true, mode, targetSpec, null);
        }

        public static ParseResult Failed(string error)
        {
            return new ParseResult(false, ResetMode.Mixed, null, error);
        }
    }
}
