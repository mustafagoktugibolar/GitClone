namespace GitClone.Application.Remote;

public enum RemoteAction
{
    Add,
    Remove,
    List
}

public sealed record RemoteRequest(string WorkingDirectory, RemoteAction Action, string? Name, string? Path) : IUseCaseRequest;
