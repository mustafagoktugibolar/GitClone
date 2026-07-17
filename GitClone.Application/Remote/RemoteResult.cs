namespace GitClone.Application.Remote;

public sealed record RemoteEntry(string Name, string Path);

public sealed record RemoteResult(
    bool Succeeded,
    RemoteAction Action,
    string Message,
    IReadOnlyList<RemoteEntry>? Remotes = null) : IUseCaseResult;
