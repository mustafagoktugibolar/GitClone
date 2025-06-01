using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class DeleteGlobalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    public bool CanExecute(string[] args)
    {
        return args.Length > 2 && ConsoleHelper.IsGlobal(args) && (args[2].Equals("remove", StringComparison.OrdinalIgnoreCase) || args[2].Equals("-rm", StringComparison.OrdinalIgnoreCase));
    }

    public void Execute(string[] args)
    {
        if (args.Length < 4)
        {
            ShowUsage("Missing command line arguments");
        }
        var removeEmail = args[3].ToLower();
        configService.RemoveGlobalConfig(removeEmail);
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
        Console.WriteLine("  ilos config --global remove <email>");
        Console.WriteLine("  ilos config --global -rm <email>");
    }
}