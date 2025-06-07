using GitClone.Helpers;
using GitClone.Interfaces;

namespace GitClone.Commands.ConfigStrategies;

public class EditLocalConfigStrategy(IConfigService configService) : IConfigStrategy
{
    public bool CanExecute(string[] args)
    {
        return args.Length > 2 && args[1].Equals("edit", StringComparison.OrdinalIgnoreCase);
    }

    public void Execute(string[] args)
    {
        if (args.Length < 4)
        {
            ShowUsage();
            return;
        };
        // get the key values
        var options = ConsoleHelper.ParseOptions(args);
        var editedUserMail = options.GetValueOrDefault("edit");
        if (editedUserMail == null)
        {
            ShowUsage("EditedUserMail is required");
            return;
        }
        
        var username = options.GetValueOrDefault("username") ?? "";
        var password = options.GetValueOrDefault("password") ?? "";
        var email = options.GetValueOrDefault("email")    ?? "";
        //TODO: handle active
        //var active = options.GetValueOrDefault("active") ?? "false";
        
        configService.EditLocalConfig(editedUserMail, username, password, email);

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
        Console.WriteLine("  ilos config edit <email> [--email <newEmail>] [--username <newUsername>] [--password]");
        Console.WriteLine("  ilos config edit <email> [--active || -a]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ilos config edit john@doe.com --username johndoe123");
        Console.WriteLine("  ilos config edit john@doe.com --email johnny@doe.com");
        Console.WriteLine("  ilos config edit john@doe.com --password");
        Console.WriteLine("  ilos config edit john@doe.com --username johndoe123 --email johnny@doe.com --password");
        Console.WriteLine("  ilos config edit john@doe.com --active | -a");
    }


}