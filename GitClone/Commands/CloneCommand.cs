using System.Windows.Input;
using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands;

public class CloneCommand(ICloneService _cloneService) : ICommandHandler
{
    public bool CanHandle(string command)
    {
        return command.Equals("clone")  || command.Equals("-cl");
    }

    public void Handle(string[] args)
    {
        try
        {
            if (args.Length < 1)
            {
                ShowUsage("Missing arguments!");
            }
            var options = ConsoleHelper.ParseOptions(args);
            var url = options.GetValueOrDefault("clone");
            var branch = options.GetValueOrDefault("branch") ?? "";
            var location = options.GetValueOrDefault("location") ?? "";
            var folderName = options.GetValueOrDefault("name") ?? "";
            if (url == null)
            {
                Console.WriteLine("url is required!");
                return;
            }
            _cloneService
                .CloneAsync(url, folderName, branch, location)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not clone repo.");
            Console.ResetColor();
            Console.WriteLine(e.Message);
        }
    }
    private void ShowUsage(string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + error);
            Console.ResetColor();
        }

        Console.WriteLine("Usage:");
        Console.WriteLine("  ilos clone <url>");
        Console.WriteLine("  ilos clone <url> --location <project destination>");
        Console.WriteLine("  ilos clone <url> --branch <branch name>");
        Console.WriteLine("  ilos clone <url> --name <project name>");
        Console.WriteLine("  ilos clone <url> --name <project name> --branch <branch name> --location <project destination>");
    }
}