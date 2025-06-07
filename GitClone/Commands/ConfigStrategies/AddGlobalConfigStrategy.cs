using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class AddGlobalConfigStrategy(IConfigService configService) : IConfigStrategy
{

    public bool CanExecute(string[] args)
    {
        return args.Length > 2 && args[2].Equals("Add", StringComparison.OrdinalIgnoreCase) && ConsoleHelper.IsGlobal(args);
    }

    public void Execute(string[] args)
    {
        if (args.Length < 3)
        {
            ShowUsage();
        }
        var username = args[3].ToLower();
        var email = args[4].ToLower();
        var password = ConsoleHelper.ReadConfirmedPassword(PasswordValidator.Validate);
        configService.AddGlobalConfig(username, email, password);
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
        Console.WriteLine("  ilos config --global add <username> <email>");
        Console.WriteLine("  ilos config --global remove <username>");
        Console.WriteLine("  ilos config --global set-active <email>");
        Console.WriteLine("  ilos config --global --list");
    }
}