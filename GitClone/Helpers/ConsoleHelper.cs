using System.Text;

namespace GitClone.Helpers;

public static class ConsoleHelper
{
    private static readonly string[] CommandLineArgs =  ["add", "a", "edit", "e", "remove", "r", "list", "l" , "clone", "-cl"];
    public static string ReadConfirmedPassword(Func<string, string?> validate)
    {
        while (true)
        {
            Console.Write("Enter password: ");
            string password = ReadPasswordFromConsole();

            string? validationMessage = validate(password);
            if (validationMessage != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(validationMessage);
                Console.ResetColor();
                continue;
            }

            Console.Write("Confirm password: ");
            string confirmPassword = ReadPasswordFromConsole();

            if (password != confirmPassword)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Passwords do not match.");
                Console.ResetColor();
                continue;
            }

            return password;
        }
    }

    private static string ReadPasswordFromConsole()
    {
        var password = new StringBuilder();


        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }

        return password.ToString();
    }
    public static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (IsKeyword(args[i]))
            {
                options[args[i]] = args[i];
            }
            else if (IsCommand(args[i]) && i + 1 < args.Length)
            {
                options[args[i]] = args[i + 1];
                i++;
            }
            else if (args[i].StartsWith("--"))
            {
                var key = args[i].TrimStart('-');

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    options[key] = args[i + 1].TrimStart('-');
                    i++;
                }
                else
                {
                    options[key] = true.ToString();
                }
            }
        }
        return options;
    }
    
    public static bool IsGlobal(string[] args)
    {
        return args.Length > 1 && args[1].Equals("--global", StringComparison.OrdinalIgnoreCase) || args[1].Equals("-g", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKeyword(string arg)
    {
        return arg.Equals("--global") || arg.Equals("-g") ;
    }

    private static bool IsCommand(string arg)
    {
        return CommandLineArgs.Contains(arg);
    }
}