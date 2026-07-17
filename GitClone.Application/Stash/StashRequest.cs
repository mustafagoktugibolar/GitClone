namespace GitClone.Application.Stash;

public enum StashAction
{
    Push,
    Pop,
    Apply,
    List,
    Drop
}

public sealed record StashRequest(
    string WorkingDirectory,
    StashAction Action,
    int Index = 0,
    string? Message = null) : IUseCaseRequest;
