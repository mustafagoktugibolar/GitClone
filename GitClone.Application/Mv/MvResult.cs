namespace GitClone.Application.Mv;

public sealed record MvResult(bool Succeeded, string Message) : IUseCaseResult;
