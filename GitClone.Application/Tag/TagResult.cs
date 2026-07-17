namespace GitClone.Application.Tag;

public enum TagOperation
{
    List,
    Create,
    Delete
}

public sealed record TagResult(
    TagOperation Operation,
    bool Succeeded,
    string Message,
    IReadOnlyList<string>? Tags = null) : IUseCaseResult;
