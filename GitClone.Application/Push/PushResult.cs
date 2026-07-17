namespace GitClone.Application.Push;

public sealed record PushResult(bool Succeeded, string Message) : IUseCaseResult;
