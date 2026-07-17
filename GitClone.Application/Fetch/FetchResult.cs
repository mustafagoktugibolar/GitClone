namespace GitClone.Application.Fetch;

public sealed record FetchResult(bool Succeeded, string Message, IReadOnlyList<string>? UpdatedRefs = null) : IUseCaseResult;
