using GitClone.Core.Abstractions;
using GitClone.Application.Clone;
using GitClone.Cli.Rendering;
using GitClone.Core.Interfaces;

namespace GitClone.Cli.Commands;

public class CloneCommand(
    CloneUseCase cloneUseCase,
    CloneRenderer renderer,
    IWorkingDirectoryProvider workingDirectoryProvider) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("clone", StringComparison.OrdinalIgnoreCase) || command.Equals("-cl", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(string[] args)
    {
        if (args.Length < 2)
        {
            renderer.RenderUsage("Missing arguments.");
            return;
        }

        if (args[1].Equals("-h", StringComparison.OrdinalIgnoreCase) ||
            args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            renderer.RenderUsage();
            return;
        }

        var options = ParseOptions(args);
        if (!options.Succeeded)
        {
            renderer.RenderUsage(options.ErrorMessage);
            return;
        }

        var request = new CloneRequest(
            workingDirectoryProvider.GetCurrentDirectory(),
            options.Url!,
            options.ProjectName,
            options.Branch,
            options.Location);

        var result = await cloneUseCase.ExecuteAsync(request);
        renderer.Render(result);
    }

    private static CloneOptions ParseOptions(string[] args)
    {
        var url = args[1];
        string? projectName = null;
        string? branch = null;
        string? location = null;

        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--name":
                    if (!TryGetOptionValue(args, ref i, out projectName))
                    {
                        return CloneOptions.Failed("Missing value for --name.");
                    }
                    break;
                case "--branch":
                    if (!TryGetOptionValue(args, ref i, out branch))
                    {
                        return CloneOptions.Failed("Missing value for --branch.");
                    }
                    break;
                case "--location":
                    if (!TryGetOptionValue(args, ref i, out location))
                    {
                        return CloneOptions.Failed("Missing value for --location.");
                    }
                    break;
                default:
                    return CloneOptions.Failed($"Unknown option: {args[i]}");
            }
        }

        return new CloneOptions(true, null, url, projectName, branch, location);
    }

    private static bool TryGetOptionValue(IReadOnlyList<string> args, ref int index, out string? value)
    {
        value = null;
        if (index + 1 >= args.Count)
        {
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private sealed record CloneOptions(
        bool Succeeded,
        string? ErrorMessage,
        string? Url,
        string? ProjectName,
        string? Branch,
        string? Location)
    {
        public static CloneOptions Failed(string error) => new(false, error, null, null, null, null);
    }
}
