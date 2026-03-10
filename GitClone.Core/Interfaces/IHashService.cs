namespace GitClone.Core.Interfaces;

public interface IHashService
{
    string ComputeSha1(string content);
    string ComputeSha256(string content);
}