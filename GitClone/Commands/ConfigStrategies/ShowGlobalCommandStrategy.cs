using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class ShowGlobalCommandStrategy(IConfigService configService) : ICommandStrategy
{
    public bool CanExecute(string[] args)
    {
        return ConsoleHelper.IsGlobal(args) && args.Length > 2 && (args[2].Equals("list", StringComparison.OrdinalIgnoreCase) || args[2].Equals("-l", StringComparison.OrdinalIgnoreCase));
    }

    public async Task Execute(string[] args)
    {
        if (args.Length < 2)
        {
            ShowUsage("Missing command line arguments");
            return;
        }
        await configService.ShowGlobalConfigs();
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