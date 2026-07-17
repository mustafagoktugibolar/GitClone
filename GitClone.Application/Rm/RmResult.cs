namespace GitClone.Application.Rm;

public sealed record RmResult(bool Succeeded, string Message) : IUseCaseResult;
