namespace GitClone.Application.Mv;

public sealed record MvRequest(string WorkingDirectory, string Source, string Destination) : IUseCaseRequest;
