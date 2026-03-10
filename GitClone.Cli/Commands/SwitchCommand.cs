using GitClone.Application.Switch;
using GitClone.Cli.Rendering;
using GitClone.Core.Abstractions;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public sealed class SwitchCommand(
    SwitchUseCase switchUseCase,
    SwitchRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("switch", StringComparison.OrdinalIgnoreCase) ||
               command.Equals("checkout", StringComparison.OrdinalIgnoreCase);
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

        var request = new SwitchRequest(
            workingDirectoryProvider.GetCurrentDirectory(),
            parsed.BranchName!,
            parsed.CreateIfMissing);

        var result = await switchUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static ParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 2)
        {
            return ParseResult.Success(args[1], false);
        }

        if (args.Count == 3 && args[1].Equals("-b", StringComparison.OrdinalIgnoreCase))
        {
            return ParseResult.Success(args[2], true);
        }

        return ParseResult.Failed("Invalid switch arguments.");
    }

    private sealed record ParseResult(bool Succeeded, string? BranchName, bool CreateIfMissing, string? ErrorMessage)
    {
        public static ParseResult Success(string branchName, bool createIfMissing)
        {
            return new ParseResult(true, branchName, createIfMissing, null);
        }

        public static ParseResult Failed(string errorMessage)
        {
            return new ParseResult(false, null, false, errorMessage);
        }
    }
}
