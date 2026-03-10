namespace GitClone.Application.Config;

public sealed record ConfigResult(
    bool Succeeded,
    bool ShowUsage,
    string? Message = null) : IUseCaseResult;
