using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class ShowLocalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    public bool CanExecute(string[] args)
    {
        return args.Length > 1 && (args[1].Equals("list", StringComparison.OrdinalIgnoreCase) || args[1].Equals("-l", StringComparison.OrdinalIgnoreCase));
    }

    public void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            ShowUsage("Missing command line arguments");
            return;
        }
        configService.ShowLocalConfigs();
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