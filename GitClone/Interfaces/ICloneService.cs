namespace GitClone.Interfaces;

public interface ICloneService
{
    Task CloneAsync(string url, string projectName, string branch, string location);
}