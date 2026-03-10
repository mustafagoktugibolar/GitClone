namespace GitClone.Application.Help;

public sealed record HelpResult(IReadOnlyList<string> Lines) : IUseCaseResult;
