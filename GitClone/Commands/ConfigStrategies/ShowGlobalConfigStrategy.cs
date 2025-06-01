using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class ShowGlobalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    public bool CanExecute(string[] args)
    {
        return ConsoleHelper.IsGlobal(args) && args.Length > 2 && (args[2].Equals("list", StringComparison.OrdinalIgnoreCase) || args[2].Equals("-l", StringComparison.OrdinalIgnoreCase));
    }

    public void Execute(string[] args)
    {
        if (args.Length < 3)
        {
            ShowUsage("Missing command line arguments");
            return;
        }
        configService.ShowGlobalConfigs();
    }

    public void ShowUsage(string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + error);
            Console.ResetColor();
        }

        Console.WriteLine("Usage:");
        Console.WriteLine("  ilos config --global list");
        Console.WriteLine("  ilos config --global -l");
    }
}