using GitClone.Application.Restore;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class RestoreCommand(
    RestoreUseCase restoreUseCase,
    RestoreRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("restore", StringComparison.OrdinalIgnoreCase);
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

        var request = new RestoreRequest(
            workingDirectoryProvider.GetCurrentDirectory(),
            parsed.TargetPath!,
            parsed.StagedOnly);

        var result = await restoreUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult Parse(IReadOnlyList<string> args)
    {
        bool stagedOnly = false;
        string? targetPath = null;

        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--staged":
                    stagedOnly = true;
                    break;
                default:
                    if (targetPath != null)
                    {
                        return ParseResult.Failed("Only a single path is supported.");
                    }

                    targetPath = args[i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return ParseResult.Failed("Path is required.");
        }

        return ParseResult.Success(targetPath, stagedOnly);
    }

    private sealed record ParseResult(bool Succeeded, string? TargetPath, bool StagedOnly, string? ErrorMessage)
    {
        public static ParseResult Success(string targetPath, bool stagedOnly)
        {
            return new ParseResult(true, targetPath, stagedOnly, null);
        }

        public static ParseResult Failed(string error)
        {
            return new ParseResult(false, null, false, error);
        }
    }
}
