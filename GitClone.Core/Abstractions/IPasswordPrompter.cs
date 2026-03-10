namespace GitClone.Core.Abstractions;

public interface IPasswordPrompter
{
    string ReadConfirmedPassword(Func<string, string?> validate);
}
