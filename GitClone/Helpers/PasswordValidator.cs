using GitClone.Interfaces;

namespace GitClone.Helpers;

public static class PasswordValidator
{
    public static string? Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password cannot be empty.";

        if (password.Length < 6)
            return "Password must be at least 6 characters long.";

        if (!password.Any(char.IsLetter))
            return "Password must contain at least one letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";

        return null; // valid
    }
}
