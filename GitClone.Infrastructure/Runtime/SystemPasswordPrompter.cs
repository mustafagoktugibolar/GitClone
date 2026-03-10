using System.Text;
using GitClone.Core.Abstractions;

namespace GitClone.Infrastructure.Runtime;

public sealed class SystemPasswordPrompter : IPasswordPrompter
{
    public string ReadConfirmedPassword(Func<string, string?> validate)
    {
        while (true)
        {
            Console.Write("Enter password: ");
            var password = ReadPasswordFromConsole();

            var validationMessage = validate(password);
            if (validationMessage != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(validationMessage);
                Console.ResetColor();
                continue;
            }

            Console.Write("Confirm password: ");
            var confirmPassword = ReadPasswordFromConsole();

            if (!password.Equals(confirmPassword, StringComparison.Ordinal))
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
            var key = Console.ReadKey(intercept: true);

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
}
