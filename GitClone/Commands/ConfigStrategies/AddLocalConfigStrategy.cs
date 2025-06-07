using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class AddLocalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    public bool CanExecute(string[] args)
    {
        return args.Length > 2 && args[1].Equals("Add", StringComparison.OrdinalIgnoreCase);
    }

    public void Execute(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }
        var username = args[2].ToLower();
        var email = args[3].ToLower();
        var password = ConsoleHelper.ReadConfirmedPassword(PasswordValidator.Validate);
        configService.AddLocalConfig(username, email, password);
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
        Console.WriteLine("  ilos config add <username> <email>");
        Console.WriteLine("  ilos config remove <username>");
        Console.WriteLine("  ilos config set-active <email>");
        Console.WriteLine("  ilos config --list");
    }
}